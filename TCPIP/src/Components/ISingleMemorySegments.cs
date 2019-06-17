namespace TCPIP
{

    // This interface keeps control of memory segments, but only one output and input element at a time.
    // This makes it possible to allocate memory without knowing the final size for the buffer
    interface ISingleMemorySegments<MetaData> where MetaData : struct
    {
        // Returns how many bytes are left in the memory
        int BytesLeft();

        // Get the amount of bytes left in the current loading segment
        int LoadDataBytesLeft();

        // Get the total amount of bytes in an load segment
        int LoadDataTotalBytes();

        // Saves the data to the current segment.
        int SaveData(int index);
        int SaveData();

        // Loads the data.
        int LoadData(int index);
        int LoadData();

        // Internal counters rolling back one or n amounts in the memory(for loadData() and SaveData())
        bool LoadDataRollback(int count = 1);
        bool SaveDataRollback(int count = 1);

        // Get the next segment
        bool NextSegment(MetaData metadata);

        // Test if the We can make a new segment
        bool NextSegmentReady();

        // Test if the load segment is ready
        bool LoadSegmentReady();

        // Test if the save segment is ready
        bool SaveSegmentReady();

        // Finish the current save or load segment, this will close of the data, set the size, and
        // make the next segment return correctly.
        // If for example, a save segment is finished, then the next block is automatically used when saving.
        // This assumes that the correct metadata is saved in the nextSegment pointer. If there are no nextSegment
        // Return false
        void FinishReadingCurrentLoadSegment();
        void FinishFillingCurrentSaveSegment();

        // Get the current meta data for loading and saving segments
        MetaData MetadataCurrentSaveSegment();
        MetaData MetadataCurrentLoadSegment();

    }
}