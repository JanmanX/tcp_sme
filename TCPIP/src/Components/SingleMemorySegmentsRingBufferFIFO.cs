namespace TCPIP
{
    // Implements a ringbus model, with segments saved in a ring like structure
    // This model guarantees a fifo queue
    public class SingleMemorySegmentsRingBufferFIFO<MetaData> : ISingleMemorySegments<MetaData> where MetaData : struct
    {
        public struct SegmentEntry{
            public int start; // Start address. Include byte
            public int stop; // Stop address, Exclude last byte
            public int current; // The current byte being worked on
            public bool filling; // The segment has been filled with data, and is ready to be loaded
            public bool reading; // The segment is getting read from
            public bool active; // Indicate if the current segment is active
            public MetaData metaData; // The meta data information
        }
        readonly int num_segments;
        readonly int memory_size;

        // Pointers to detect head and tail of the ringbuffer, and where we have made new segments
        private int save_segment_id = 0;
        private int load_segment_id = 0;
        private int next_segment_id = 0;
        private SegmentEntry[] segment_list;



        ////////// interface
        public SingleMemorySegmentsRingBufferFIFO(int num_segments, int memory_size)
        {
            this.num_segments = num_segments;
            this.memory_size = memory_size;
            // Allocate the ring entry segments
            this.segment_list = new SegmentEntry[num_segments];
            // Set default values
            for (int i = 0; i < num_segments; i++)
            {
                SegmentEntry x = segment_list[i];
                // new segments have no range 0 to 0, and is therefore filled up, and have been read,
                // so we can detect they are free.
                x.filling = false;
                x.active = false;
                x.reading = false;
                x.start = 0;
                x.stop = 0;
                segment_list[i] = x;
            }
        }

        public int BytesLeft()
        {
            throw new System.NotImplementedException();
        }

        public int LoadDataBytesLeft()
        {
            SegmentEntry current = segment_list[load_segment_id];
            return MemoryRange(current.start,current.stop) - current.current;
        }

        public int LoadDataTotalBytes()
        {
            SegmentEntry current = segment_list[load_segment_id];
            return MemoryRange(current.start,current.stop);
        }

        public int SaveData(int index)
        {
            SegmentEntry current = segment_list[save_segment_id];

            // If the current block is active, but filling mode have not been enabled, we set the
            // stop byte to look at the last segment, and set the filling byte
            if(current.active && !current.filling && !current.reading)
            {
                // find the last segment id, and tset the start byte to the stop byte of the last
                int last_save_segment_id = save_segment_id - 1 < 0 ? this.num_segments - 1: save_segment_id - 1;
                SegmentEntry last = segment_list[last_save_segment_id];
                current.start = last.stop;
                current.stop = last.stop;
                current.filling = true;
                Logging.log.Info($"Is filling");
            }
            // The segment is not in filling mode! something went wrong
            if(!current.filling)
            {
                throw new System.Exception("The segment is not in filling mode! we cannot save to it");
            }

            int addr = (current.start + index) % memory_size;
            // Test if we should increase max size of element

            if(MemoryRange(current.start,current.stop) <= MemoryRange(current.start,addr))
            {
                current.stop = (addr + 1) % memory_size;
                segment_list[save_segment_id] = current;
            }
            return addr;
        }

        public int SaveData()
        {
            // increment the current counter, and get address
            SegmentEntry current = segment_list[save_segment_id];
            int x = current.current;
            int ret = SaveData(x);
            if(ret != -1){
                current = segment_list[save_segment_id];
                current.current++;
            }
            segment_list[save_segment_id] = current;
            return ret;

        }

        public int LoadData(int index)
        {
            SegmentEntry current = segment_list[load_segment_id];
            if(!current.reading)
            {
                //Logging.log.Warn($"The segment {load_segment_id} is not in reading mode! we cannot load from it");
                return -1;
            }
            int addr = (current.start + index) % memory_size;
            // The memory is out of range
            if(MemoryRange(current.start,current.stop) <= MemoryRange(current.start,addr))
            {

                throw new System.Exception("Requesting for memory out of index for that block!");
            }
            return addr;
        }

        public int LoadData()
        {
            // increment the current counter, and get address
            SegmentEntry current = segment_list[load_segment_id];
            int x = current.current;
            int ret = LoadData(x);
            if(ret != -1){
                current = segment_list[load_segment_id];
                current.current++;
            }
            segment_list[load_segment_id] = current;
            return ret;
        }

        public bool LoadDataRollback(int count = 1)
        {
            return Rollback(load_segment_id,count);
        }
        public bool SaveDataRollback(int count = 1)
        {
            return Rollback(save_segment_id,count);
        }

        public bool NextSegment(MetaData metadata)
        {
            // test if the next segment is good, else return false
            SegmentEntry next = segment_list[next_segment_id];
            // The segment is being filled or read from, return error
            if(next.active)
            {
                //throw new System.Exception("the next segment is already active! ");
                return false;
            }
            next.start = 0;
            next.stop = 0;
            next.filling = false;
            next.reading = false;
            next.active = true;
            next.metaData = metadata;
            segment_list[next_segment_id] = next;
            next_segment_id = (next_segment_id + 1) % num_segments;

            return true;
        }

        public bool NextSegmentReady()
        {
            // Test if the next segment is ready
            SegmentEntry next = segment_list[next_segment_id];
            return !next.active;
        }

        public bool LoadSegmentReady()
        {
            SegmentEntry current = segment_list[load_segment_id];
            return !current.filling && current.reading && current.active;
        }
         public bool SaveSegmentReady()
        {
            SegmentEntry current = segment_list[save_segment_id];
            return !current.reading && current.active;
        }

        public void FinishReadingCurrentLoadSegment()
        {
            // We have now filled and read the segment, mark it as inactive
            SegmentEntry current = segment_list[load_segment_id];
            current.reading = false;
            current.active = false;
            current.current = 0;
            segment_list[load_segment_id] = current;
            // Increment the load segment id
            load_segment_id = (load_segment_id + 1) % num_segments;
            // Indicate that the next block is ready to be loaded
            // XXX : Maybe test if we can do this?
            SegmentEntry next = segment_list[load_segment_id];
            next.reading = true;
            segment_list[load_segment_id] = next;
        }

        public void FinishFillingCurrentSaveSegment()
        {
            SegmentEntry current = segment_list[save_segment_id];
            current.filling = false;
            current.reading = true;
            current.current = 0;
            segment_list[save_segment_id] = current;
            // Increment the load segment id
            save_segment_id = (save_segment_id + 1) % num_segments;
            // Indicate that the next block is ready to be filled
            // XXX : Maybe test if we can do this?
            SegmentEntry next = segment_list[save_segment_id];
            next.filling = true;
            segment_list[save_segment_id] = next;
        }

        public MetaData MetadataCurrentSaveSegment()
        {
            return segment_list[load_segment_id].metaData;
        }

        public MetaData MetadataCurrentLoadSegment()
        {
            return segment_list[save_segment_id].metaData;
        }

        //////// helping functions
        // Get the length between two addresses
        private int MemoryRange(int start, int stop){
            // if the start is higher than the stop, it must wrap around
            if (start > stop){
                return memory_size - (start - stop);
            } else{
                return stop - start;
            }
        }
        private bool Rollback(int segment_id, int count){
            SegmentEntry current = segment_list[segment_id];
            current.current -= count;
            segment_list[segment_id] = current;
            return true;
        }
    }
}
