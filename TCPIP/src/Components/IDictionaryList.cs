namespace TCPIP
{
    // Interface that defines how to handle a dictionary (key:[value]), where the
    // value is an ordered list. The allocated amount is ofcourse static.
    // The storage itself is not handled here, but pointers to the underlaying memory implementation
    // are returned

    interface IDictionaryList
    {
        // How many Addresses are left we can save in?
        int ValueSpaceLeft();

        // How many spaces are left in the key table?
        int KeySpaceLeft();

        // Add a new key, return true if added, otherwise false
        bool New(int key);

        // Delete a key (free up memory), Return true if deleted, otherwise false
        bool Free(int key);

        // Do we already have an key?
        bool ContainsKey(int key);

        // Get first reserved key
        int GetFirstKey();

        // Gets the first value from a key list
        int GetFirstValue(int key);

        // Get the length of the list
        int ListLength(int key);

        // Modifiers for the keystore.

        // Insert inserts an element at that specific index, if it already exist,
        // the same pointer is returned
        int Insert(int key, int index);

        // Deletes an element in the array
        int Delete(int key, int index);

        // Observe returns the pointer of that specific index, and nothing else.
        // If the value does not exist, return -1 as value pointer
        int Observe(int key, int index);


    }
}