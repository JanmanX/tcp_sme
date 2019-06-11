namespace TCPIP
{
    // Interface that defines how to handle multiple memory in segments at once,
    // with multiple saves and loads on segments out of order.
    // The memory savings itself are not handled here, only addres are returned, and
    // the process which recives the address must save the data themselves

    // The segments also handles meta data, which are additional information per segment,
    // that can be accessed instantly

    // This interface can handle multiple elements at the same time, but
    // requires the size of the segment beforehand

    // If we do not know the size of the element, but only have one element at a time
    // use the ISingleMemorySegments.

    interface IMultiMemorySegments<MetaData> where MetaData : struct
    {
        // How many segments of memory are left?
        int SegmentsLeft();

        // Get the address we have to save the data inside. Throw error if segment is set to "Full"
        // Data saved without the offset will automatically increase the returned
        // address for the next call. When the last byte is set, the segment will be
        // marked as "Full" automatically.
        // Data saved with hardcoded offset will not increase the address by itself, or set the "Full" flag
        int SaveData(int segment_ID, int offset);
        int SaveData(int segment_ID);

        // Get the address for that specific segment. Throw error if loading data from an not "Done" segment
        // Loading data without offset will automatically increase the returned
        // address for the next call. When the last byte is loaded, the segment will be
        // marked as "Done" automatically.
        // Data loaded with offset will not increase the address by itself, or set the "Done" flag
        int LoadData(int segment_ID, int offset);
        int LoadData(int segment_ID);

        // Allocate a segment and return the segment_ID. The segment_ID May be
        // reused if an old segment is marked as done
        int AllocateSegment(int size);

        // Returns the next segment to consume based on whats best from the
        // underlaying implementation(returns correct order if FIFO implementation etc.)
        int FocusSegment();

        // Get information from the segment, if it is full, done etc.
        bool IsSegmentDone(int segment_ID);
        bool IsSegmentFull(int segment_ID);
        // Bytes left in the segment
        int SegmentBytesLeft(int segment_ID);

        // Mark a segment "Done". This frees up a memory segment, can only be done if segment is marked as full
        void SegmentDone(int segment_ID);

        // Mark a segment "Full".
        void SegmentFull(int segment_ID);

        // Rollback the internal segment counter, if for example an address has been
        // Loaded or Saved, but the operation to the memory did not happen due to canceled transaction.
        // It is the same counter used for SegmentBytesleft, and Load and Save when no offset is set.
        void SegmentRollback(int segment_ID, int count = 1);

        // Set and get the meta data for an specific element
        void SaveMetaData(int segment_ID, MetaData meta_data);
        MetaData LoadMetaData(int segment_ID);

    }
}