namespace TCPIP
{
    // Implements a ringbus model, with segments saved in a ring like structure
    // This model guarantees a fifo queue
    public class MultiMemorySegmentsRingBufferFIFO<MetaData> : IMultiMemorySegments<MetaData> where MetaData : struct
    {
        public struct SegmentEntry{
            public int start; // Start address. Include byte
            public int stop; // Stop address, Exclude last byte
            public int current; // The current byte being written
            public bool done; // The segment has been fully read
            public bool full; // The segment is full
            public MetaData metaData; // The meta data information
        }
        readonly int num_segments;
        readonly int memory_size;

        // Pointers to detect head and tail of the ringbuffer
        private int head_segment_id = 0;
        private int tail_segment_id = 0;
        private SegmentEntry[] segment_list;



        ////////// interface
        public MultiMemorySegmentsRingBufferFIFO(int num_segments,int memory_size)
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
                x.done = true;
                x.full = true;
                x.start = 0;
                x.stop = 0;
                segment_list[i] = x;
            }
        }

        public int SegmentsLeft()
        {
            int ret = 0;
            // Iterate over the ringbus and accumulate all done RingEntry's
            for (int i = 0; i < num_segments; i++)
            {
                ret += segment_list[i].done && segment_list[i].full  ? 1 : 0;
            }
            return ret;
        }

        public int SaveData(int segment_ID, int offset)
        {
            // Get the current segment
            SegmentEntry cur_segment = segment_list[segment_ID];
            if (cur_segment.full){
                throw new System.Exception($"Segment ID:{segment_ID} is marked as full, so data cannot be saved");
            }
            return (cur_segment.start + offset) % memory_size;
        }

        public int SaveData(int segment_ID)
        {
            // Get the current segment
            SegmentEntry cur_segment = segment_list[segment_ID];
            if (cur_segment.full){
                throw new System.Exception($"Segment ID:{segment_ID} is marked as full, so data cannot be saved");
            }
            // Calculate the offset
            int ret = (cur_segment.start + cur_segment.current) % memory_size;
            cur_segment.current++;
            // If the next byte is the last, we mark the segment as full, and reset counters
            if (cur_segment.current % memory_size == cur_segment.stop){
                SegmentFull(segment_ID);
                cur_segment.current = 0;
            }
            // Put the data back
            segment_list[segment_ID] = cur_segment;
            return ret;
        }

        public int LoadData(int segment_ID, int offset = -1)
        {
            // Get the current segment
            SegmentEntry cur_segment = segment_list[segment_ID];
            if (cur_segment.full && !cur_segment.done){
                throw new System.Exception($@"Segment ID:{segment_ID}
                                              should be Full=True, Done=False,
                                              but is Full={cur_segment.full}, Done={cur_segment.done}.
                                              Data cannot be loaded");
            }
            return (cur_segment.start + offset) % memory_size;
        }

        public int LoadData(int segment_ID)
        {
            // Get the current segment
            SegmentEntry cur_segment = segment_list[segment_ID];
            if (cur_segment.full && !cur_segment.done){
                throw new System.Exception($@"Segment ID:{segment_ID}
                                              should be Full=True, Done=False,
                                              but is Full={cur_segment.full}, Done={cur_segment.done}.
                                              Data cannot be loaded");
            }
            // Calculate the offset
            int ret = (cur_segment.start + cur_segment.current) % memory_size;
            cur_segment.current++;
            // If the next byte is the last, we mark the segment as full, and reset counters
            if (cur_segment.current % memory_size == cur_segment.stop){
                SegmentDone(segment_ID);
                cur_segment.current = 0;
            }
            // Put the data back
            segment_list[segment_ID] = cur_segment;
            return ret;
        }

        public int AllocateSegment(int size)
        {
            int last_segment_id = head_segment_id;
            int new_segment_id = (last_segment_id + 1) % num_segments;
            SegmentEntry last_segment = segment_list[last_segment_id];
            SegmentEntry new_segment = segment_list[new_segment_id];
            SegmentEntry tail_segment = segment_list[tail_segment_id];

            // If the next segment is not done, the buffer is filled
            if(!new_segment.done)
            {
               throw new System.Exception("The segment entry table is full!");
            }
            // If the range is currently bigger than what we can handle, there is nothing to do
            if (MemoryRange(last_segment.stop, tail_segment.start) < size){
                throw new System.Exception("The range is not big enough for the allocation");
            }
            new_segment.done = false;
            new_segment.full = false;
            new_segment.start = last_segment.stop;
            // Offset with last byte
            new_segment.stop = (new_segment.start + size + 1) % memory_size;
            new_segment.current = 0;
            // save the segment
            segment_list[new_segment_id] = new_segment;
            // set the head segment to a new segment
            head_segment_id = new_segment_id;
            return new_segment_id;
        }

        public bool IsSegmentDone(int segment_ID)
        {
            return segment_list[segment_ID].done;
        }

        public bool IsSegmentFull(int segment_ID)
        {
            return segment_list[segment_ID].full;
        }

        public int SegmentBytesLeft(int segment_ID)
        {
            SegmentEntry cur_segment = segment_list[segment_ID];
            return MemoryRange(cur_segment.current, cur_segment.stop);
        }

        public void SegmentDone(int segment_ID)
        {
            SegmentEntry cur_segment = segment_list[segment_ID];
            if (!cur_segment.full){
                throw new System.Exception($@"Segment ID:{segment_ID} Cannot mark as done, when it is not full first");
            }
            cur_segment.done = true;
            segment_list[segment_ID] = cur_segment;
            // See if we should progress the tail pointer;
            while(segment_list[tail_segment_id].done){
                tail_segment_id = (tail_segment_id + 1) % num_segments;
            }
        }

        public void SegmentFull(int segment_ID)
        {
            SegmentEntry cur_segment = segment_list[segment_ID];
            cur_segment.full = true;
            segment_list[segment_ID] = cur_segment;
        }

        public void SegmentRollback(int segment_ID,int count = 1){
           SegmentEntry cur_segment = segment_list[segment_ID];
            cur_segment.current -= count;
            segment_list[segment_ID] = cur_segment;
        }

        public void SaveMetaData(int segment_ID, MetaData meta_data)
        {
            SegmentEntry cur_segment = segment_list[segment_ID];
            cur_segment.metaData = meta_data;
            segment_list[segment_ID] = cur_segment;
        }

        public MetaData LoadMetaData(int segment_ID)
        {
            return segment_list[segment_ID].metaData;
        }

        public int FocusSegment()
        {
            return tail_segment_id;
        }

        /////// Helper functions
        // Get amount of memory between range
        private int MemoryRange(int start, int stop){
            // if the start is higher than the stop, it must wrap around
            if (start > stop){
                return memory_size - (start - stop);
            } else{
                return stop - start;
            }
        }
    }
}
