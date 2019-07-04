namespace TCPIP
{
    // This class have a 1 to 1 list with keys to initial pointers to a data storage.
    // There are also a list containing the actual addresses. This list contains multiple
    // linked lists, with possibility of sparseness via index offsetters.
    // If a address is requested from an sparse area, an error address (-1) is returned
    public class DictionaryListSparseLinked<MetaData> : IDictionaryList<MetaData> where MetaData : struct {
        // Used to return link information

        // The key translation struct is used to look up where to start the pointer
        // lookup from that specific key. This makes it possible to have arbitary keys.
        public struct KeyTranslation{
            public int key; // The key to use
            public int index; // The pointer to the LinkEntry list, is it -1, then it is not allocated
            public int offset; // Initial offset for the root block
            public bool used; // is this currently used?
            public MetaData meta_data;
        }
        // The link entry contains what to look at previous and next in the cain
        // The index itself in the LinkEntry list is the returned address
        public struct LinkEntry{
            public int next; // Internal pointer to next element, is -1 when tail
            // The offset variable for sparse calculations. 0 offset means that the next block is 1 normal index away etc
            public int offset;
            public bool used; // Is this LinkEntry currently used?
        }

        private LinkEntry[] links;
        // Contains the last link pointer used
        private int last_link_index = 0;

        private KeyTranslation[] keys;

        // Contains the last key pointer allocated to a key
        private int last_key_pointer = 0;



        public DictionaryListSparseLinked(int total_list_length) : this(total_list_length,total_list_length){}

        public DictionaryListSparseLinked(int key_list_length,int total_list_length)
        {
            this.keys = new KeyTranslation[key_list_length];
            this.links = new LinkEntry[total_list_length];

            // Reset initial data
            for (int i = 0; i < this.links.Length; i++)
            {
                LinkEntry x = links[i];
                x.next = -1;
                x.offset = 0;
                x.used = false;
                links[i] = x;
            }
            // Reset initial data
            for (int i = 0; i < this.keys.Length; i++)
            {
                KeyTranslation x = keys[i];
                x.used = false;
                x.index =  -1;
                keys[i] = x;
            }
        }

        // Iterate over the links list and count how much free space we have
        public int ValueSpaceLeft()
        {
            int ret = 0;
            for (int i = 0; i < this.links.Length; i++)
            {
                ret += !links[i].used ? 1 : 0;
            }
            return ret;
        }

        // Iterate over the keys list and count how much free space we have
        public int KeySpaceLeft()
        {
            int ret = 0;
            for (int i = 0; i < this.keys.Length; i++)
            {
                ret += !keys[i].used ? 1 : 0;
            }
            return ret;
        }

        public bool New(int key)
        {
            int count = 0;
            // Limit to checking all keys only once
            while(count++ < this.keys.Length){

                // Test if the current free pointer is free
                if(!keys[last_key_pointer].used){
                    KeyTranslation x = keys[last_key_pointer];
                    x.used = true;
                    x.key = key;
                    x.index = -1;
                    keys[last_key_pointer] = x;
                    Logging.log.Info($"New key: {key} at key pointer: {last_key_pointer}");
                    return true;
                }
                last_key_pointer = (last_key_pointer + 1 ) % keys.Length;
            }
            return false;
        }

        // Mark the current key as free, and therefore release all list data
        public bool Free(int key)
        {
            int key_pointer = GetKeyPointer(key);
            if (key_pointer == -1){
                throw new System.Exception("Key does not exist");
                return false;
            }
            KeyTranslation x = keys[key_pointer];
            x.used = false;
            keys[key_pointer] = x;
            TraverseLinkPointerAndFree(keys[key_pointer].index);
            return true;

        }

        // Do we already have an key?
        public bool ContainsKey(int key){
            return GetKeyPointer(key) != -1;
        }


        public int GetFirstKey()
        {
            for (int i = 0; i < this.keys.Length; i++)
            {
                if(keys[i].used)
                {
                    return i;
                }
            }
            return -1;
        }

        // Get the first value form list in the key:[list]
        public int GetFirstValue(int key)
        {
             int key_pointer = GetKeyPointer(key);
            if (key_pointer == -1){
                throw new System.Exception("Key does not exist");
                return -1;
            }
            KeyTranslation x = keys[key_pointer];
            return x.index;
        }

        // Get the length of the list
        public int ListLength(int key){
            int key_pointer = GetKeyPointer(key);
            if (key_pointer == -1){
                throw new System.Exception("Key does not exist");
                return -1;
            }
            KeyTranslation k = keys[key_pointer];
            // if the list is 0 long, return as such
            if(k.index == -1){
                return 0;
            }
            int last_index = TraverseLinkPointerToEnd(k.index);

            return DistancebetweenLinks(k.index,last_index) + k.offset;
        }


        public int Insert(int key, int index)
        {
            int key_pointer = GetKeyPointer(key);
            int link_index_new = -1;
            if(key_pointer == -1){
                throw new System.Exception("Key does not exist");
            }
            KeyTranslation k = keys[key_pointer];
            // The pointer in the key table is negative 1, therefore we need a new module.
            // We create this and returns and saves the pointer
            if(k.index == -1)
            {
                link_index_new = GetFreeLinkAndReserve();
                if(link_index_new == -1)
                {
                    throw new System.Exception("Link could not be allocated");
                }

                // Set the key
                k.index = link_index_new;
                k.offset = index;
                keys[key_pointer] = k;

                // Set the list link
                LinkEntry link_node_new = links[link_index_new];
                link_node_new.used = true;
                link_node_new.offset = 0;
                link_node_new.next = -1;
                links[link_index_new] = link_node_new;
                return link_index_new;
            }

            int root_index = k.index;
            LinkEntry root_node = links[root_index];

            // There are now 4 cases:
            // * the new node is the very first (Not hitting an node)
            // * the node is somewhere in the center of it all(not hitting an node)
            // * the node is the very last(Not hitting an node)
            // * the node hits an already existing node

            // Test if the node actually exist
            // Remove the key initial offset
            int link_index_exact = TraverseLinkPointerExact(k.index,index - k.offset);
            // If it is not -1, then it must exist and be correct, therefore return
            if(link_index_exact != -1){
                return link_index_exact;
            }
            // We now know that the root key element points to an element, and
            // we are not hitting any elements directly
            // We find the element before the next existing element

            int link_index_before = TraverseLinkPointerBefore(k.index, index - k.offset);

            // If there exist no before element, we are the new root, change accordingly
            if (link_index_before == -1)
            {
                // Get a new link
                link_index_new = GetFreeLinkAndReserve();

                // Set the key
                k.index = link_index_new;
                k.offset = index;
                keys[key_pointer] = k;

                // Set the list link
                LinkEntry link_node_new = links[link_index_new];
                link_node_new.used = true;
                link_node_new.offset = k.offset - index;
                link_node_new.next = root_index;
                links[link_index_new] = link_node_new;
                return link_index_new;
            }
            // There exist an before element, we merge between the before and next node
            else
            {
                // Get a new link
                link_index_new = GetFreeLinkAndReserve();

                LinkEntry link_node_before = links[link_index_before];
                // Calculate the link distance from the root offset to the before node
                int link_node_before_depth = DistancebetweenLinks(k.index,link_index_before) + k.offset;
                link_node_before.used = true;
                int link_node_before_offset = link_node_before.offset;
                link_node_before.offset = index - link_node_before_depth ;
                int link_node_before_next = link_node_before.next;
                link_node_before.next = link_index_new;
                links[link_index_before] = link_node_before;

                // Set the list link
                LinkEntry link_node_new = links[link_index_new];
                link_node_new.used = true;
                link_node_new.offset = link_node_before_offset + link_node_before_depth - index; //link_node_before_offset - link_node_before.offset;
                link_node_new.next = link_node_before_next;
                links[link_index_new] = link_node_new;
                return link_index_new;
            }
        }


        public int Delete(int key, int index)
        {
            // XXXXXX Possible edgecases
            Logging.log.Fatal($"Delete called on: key: {key} index: {index}");
            int key_pointer = GetKeyPointer(key);
            if(key_pointer == -1){
                throw new System.Exception("Key does not exist");
            }
            KeyTranslation k = keys[key_pointer];

            // If the key is empty, we cant delete stuff
            if(k.index == -1)
            {
                Logging.log.Warn($"The list is empty! key pointer: {key_pointer}");
                return -1;
            }

            // Get the exact link, if it is not -1, it is not sparse, and we must remove it
            int link_index_exact = TraverseLinkPointerExact(k.index,index - k.offset);
            // If it is not -1, then it must exist and be correct, therefore return
            if(link_index_exact == -1){
                return link_index_exact;
            }
            // If it is the root node, we replace the lookup in keys
            if(index == k.offset){

                // Get the delete node
                LinkEntry delete_node = links[link_index_exact];

                // Set the key
                k.index = delete_node.next;
                k.offset =  delete_node.offset + k.offset; //index + k.offset; // delete_node.offset + k.offset;
                keys[key_pointer] = k;

                // Delete it
                delete_node.used = false;
                delete_node.next = -1;
                delete_node.offset = 0;
                links[link_index_exact] = delete_node;
                return index;
            }
            // Is not an root node, it must have an before node, we scope that and shuffle around!
            else{
                // Delete the node
                LinkEntry delete_node = links[index];
                delete_node.used = false;
                delete_node.next = -1;
                delete_node.offset = 0;
                links[index] = delete_node;

                // the before node
                int link_index_before = TraverseLinkPointerBefore(k.index, index - k.offset);
                LinkEntry before_node = links[link_index_before];
                before_node.used = true;
                before_node.offset = before_node.offset + delete_node.offset;
                before_node.next = delete_node.next;
                links[link_index_before] = before_node;
                return index;
            }
        }



        public int Observe(int key, int index)
        {
            int key_pointer = GetKeyPointer(key);
            if(key_pointer == -1){
                throw new System.Exception("Key does not exist");
            }
            KeyTranslation k = keys[key_pointer];
            // With the pointer from the key lookup, we can traverse the link list
            return TraverseLinkPointerExact(k.index, index - k.offset);
        }

        public void SaveMetaData(int key, MetaData meta_data){
            int key_pointer = GetKeyPointer(key);
            if(key_pointer == -1){
                throw new System.Exception("Key does not exist");
            }
            KeyTranslation k = keys[key_pointer];
            k.meta_data = meta_data;
            keys[key_pointer] = k;
        }
        public MetaData LoadMetaData(int key){
            int key_pointer = GetKeyPointer(key);
            if(key_pointer == -1){
                throw new System.Exception("Key does not exist");
            }
            KeyTranslation k = keys[key_pointer];
            return k.meta_data;
        }

        ////////////// Helper functions

        // Looks for the next free link, and reserves it
        private int GetFreeLinkAndReserve()
        {
            int count = 0;
            // Limit to checking all keys only once
            while(count++ < links.Length){
                last_link_index = (last_link_index + 1 ) % links.Length;
                // Test if the current pointer is not used
                if(!links[last_link_index].used){
                    // Set it to used, reset stuff and return link pointer
                    LinkEntry x = links[last_link_index];
                    x.used = true;
                    x.next = -1;
                    x.offset = 0;
                    links[last_link_index] = x;
                    return last_link_index;
                }
            }
            return -1;
        }
        // Get the index form the key table
        private int GetKeyPointer(int key)
        {
            for (int i = 0; i < this.keys.Length; i++)
            {
                if(keys[i].key == key && keys[i].used)
                {
                    return i;
                }
            }
            return -1;
        }

        // Get the correct link pointer, and return -1 if error
        // index is the initial index of the element, the depth is the traversal
        // steps of the element.
        // This function assumes that the index is correct
        private int TraverseLinkPointerExact(int index, int depth)
        {
            for(int j = 0; j < links.Length; j++)
            {
                // If the depth is 0, the link must be the same
                if(depth == 0){
                    return index;
                }
                // if depth is deeper than 0 (-n), then we must have overshoot, and
                // Link does not exist, or are sparse
                if(depth < 0){
                    return -1;
                }

                LinkEntry x = links[index];
                if(!x.used){
                    throw new System.Exception("Iterating over not used element, Something is wrong!");
                }

                if(x.next == -1){
                    return -1;
                }
                // if the next element is not negative, we can traverse further
                index = x.next;
                depth = depth - x.offset;
            }

            return -1;
       }


        // Overloaded operator for mapping of arguments
        private int TraverseLinkPointerBefore(int index, int depth)
        {
            // Set last index to -1, since it is impossible to find the
            // root node from just the index.
            return TraverseLinkPointerBefore(index, -1, depth);
        }
        // Get the link pointer before the actual depth we want
        private int TraverseLinkPointerBefore(int index, int last_index, int depth)
        {
            for(int j = index; j < links.Length; j++)
            {
                LinkEntry x = links[index];

                // If the depth is 0 or below, the link must be the one before
                if(depth <= 0){
                    return last_index;
                }

                if(!x.used){
                    throw new System.Exception("Iterating over not used element, Something is wrong!");
                }

                // if the next element is not negative, we can traverse further
                if(x.next == -1)
                {
                    // we are at positive depth, but there exist no more elements, this must be the before
                    // since we are overshooting into non existant element
                    return index;
                }


                depth = depth - x.offset;
                last_index = index;
                index = x.next;
            }
            return -1;
       }


        // Traverse the linkpointer and free up all seen elements.
        // Used for cleaning up after key removal
        private void TraverseLinkPointerAndFree(int index)
        {
            for(int i = 0; i < links.Length; i++)
            {
                LinkEntry x = links[(i + index) % links.Length];
                x.used = false;
                x.next = -1;
                x.offset = 0;
                links[(i + index) % links.Length] = x;
                if(x.next == -1 ){
                    break;
                }
            }

       }


        // Returns the last linkpointer in a chain
        private int TraverseLinkPointerToEnd(int index)
        {
            for(int i = 0; i < links.Length; i++)
            {
                LinkEntry x = links[index];
                if(x.next == -1) {
                    break;
                }

                index = x.next;
            }

            return index;
        }

        // calculate the spacing from node a to node b(with offset calculations).
        // Remember to add the root key offset (If array first contains something at 5 etc)
        // the indexes must exist(not sparse elements)
        private int DistancebetweenLinks(int index_a, int index_b)
        {
            int acc = 0;
            for(uint i = 0; i < links.Length; i++)
            {
                if (index_a == index_b)
                {
                    break;
                }

                LinkEntry x = links[index_a];

                acc += x.offset;

                index_a = x.next;
            }

            return acc;
        }
    }
}
