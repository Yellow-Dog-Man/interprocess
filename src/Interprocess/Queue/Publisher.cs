namespace Cloudtoid.Interprocess;

internal sealed class Publisher : Queue, IPublisher
{
    private readonly IInterprocessSemaphoreReleaser signal;

    internal Publisher(QueueOptions options, ILoggerFactory loggerFactory)
        : base(options, loggerFactory) => signal = InterprocessSemaphore.CreateReleaser(options.MemoryViewName);

    public unsafe bool TryEnqueue(ReadOnlySpan<byte> message)
    {
        var bodyLength = message.Length;
        var messageLength = GetPaddedMessageLength(bodyLength);

        while (true)
        {
            var header = *Header;

            if (!CheckCapacity(header, messageLength))
                return false;

            var writeOffset = header.WriteOffset;
            var newWriteOffset = SafeIncrementMessageOffset(writeOffset, messageLength);

            // try to atomically update the write-offset that is stored in the queue header
            if (Interlocked.CompareExchange(ref Header->WriteOffset, newWriteOffset, writeOffset) == writeOffset)
            {
                try
                {
                    // Write the header first with the bodyLength included. This is important, because writing the
                    // header is not an atomic operation, so we want the bodyLength to be staged and ready first,
                    // because once we write ReadyToBeConsumedState, the Subscriber can start processing the message
                    // before we write out the bodyLength, resulting in corruption of the queue
                    Buffer.Write(
                        new MessageHeader(MessageHeader.WritingState, bodyLength),
                        writeOffset);

                    // write the message body
                    Buffer.Write(message, GetMessageBodyOffset(writeOffset));

                    // Update the message header with ReadyToBeConsumedState since we've written the entire body of the message.
                    // The bodyLength is already staged form initial write, so we don't need to write it again (in fact, we shouldn't,
                    // because it's technically possible that the Subscriber will process the message and zero out the data before
                    // we write the bodyLength, meaning we'd write the bodyLength to already zeroed out buffer, since this is not atomic
                    Buffer.Write(MessageHeader.ReadyToBeConsumedState, writeOffset);
                }
                catch
                {
                    // if there is an error here, we are in a bad state.
                    // treat this as a fatal exception and crash the process
                    Environment.FailFast(
                        "Publishing to the shared memory queue failed leaving the queue in a bad state. The only option is to crash the application.");
                }

                // signal the next receiver that there is a new message in the queue
                signal.Release();
                return true;
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            signal.Dispose();

        base.Dispose(disposing);
    }

    private bool CheckCapacity(QueueHeader header, long messageLength)
    {
        if (messageLength > Buffer.Capacity)
            return false;

        if (header.IsEmpty())
            return true; // it is an empty queue

        var readOffset = header.ReadOffset % Buffer.Capacity;
        var writeOffset = header.WriteOffset % Buffer.Capacity;

        if (readOffset == writeOffset)
            return false; // queue is full

        if (readOffset < writeOffset)
        {
            if (messageLength > Buffer.Capacity + readOffset - writeOffset)
                return false;
        }
        else if (messageLength > readOffset - writeOffset)
        {
            return false;
        }

        return true;
    }
}