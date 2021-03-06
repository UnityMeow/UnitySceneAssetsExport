using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System.Threading;
using System.Runtime.CompilerServices;

public unsafe struct NativeList_Int : IEnumerable<int>
{
    [NativeDisableUnsafePtrRestriction]
    private NativeListData* data;
    public bool isCreated { get; private set; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeList_Int(int capacity, Allocator alloc)
    {
        isCreated = true;
        capacity = Mathf.Max(capacity, 1);
        data = MUnsafeUtility.Malloc<NativeListData>(sizeof(NativeListData), alloc);
        data->count = 0;
        data->capacity = capacity;
        data->allocator = alloc;
        data->ptr = MUnsafeUtility.Malloc<int>(sizeof(int) * capacity, alloc);
    }
    public NativeList_Int(int count, Allocator alloc, int defaultValue)
    {
        isCreated = true;
        data = MUnsafeUtility.Malloc<NativeListData>(sizeof(NativeListData), alloc);
        data->count = count;
        data->capacity = count;
        data->allocator = alloc;
        data->ptr = MUnsafeUtility.Malloc<int>(sizeof(int) * count, alloc);
        int* add = (int*)data->ptr;
        for (int i = 0; i < count; ++i)
        {
            add[i] = defaultValue;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeList_Int(int count, int capacity, Allocator alloc)
    {
        isCreated = true;
        data = MUnsafeUtility.Malloc<NativeListData>(sizeof(NativeListData), alloc);
        data->count = count;
        data->capacity = capacity;
        data->allocator = alloc;
        data->ptr = MUnsafeUtility.Malloc<int>(sizeof(int) * capacity, alloc);
    }
    public Allocator allocator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return data->allocator;
        }
    }
    private void Resize()
    {
        if (data->count <= data->capacity) return;
        data->capacity *= 2;
        void* newPtr = MUnsafeUtility.Malloc<int>(sizeof(int) * data->capacity, data->allocator);
        UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(int) * data->count);
        UnsafeUtility.Free(data->ptr, data->allocator);
        data->ptr = newPtr;
    }

    private void ResizeToCount()
    {
        if (data->count <= data->capacity) return;
        int oldCap = data->capacity;
        data->capacity = data->count;
        void* newPtr = MUnsafeUtility.Malloc<int>(sizeof(int) * data->capacity, data->allocator);
        UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(int) * oldCap);
        UnsafeUtility.Free(data->ptr, data->allocator);
        data->ptr = newPtr;
    }

    public void AddCapacityTo(int capacity)
    {
        if (capacity <= data->capacity) return;
        data->capacity = capacity;
        void* newPtr = MUnsafeUtility.Malloc<int>(sizeof(int) * data->capacity, data->allocator);
        UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(int) * data->count);
        UnsafeUtility.Free(data->ptr, data->allocator);
        data->ptr = newPtr;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveLast(int length)
    {
        data->count -= length;
        data->count = Mathf.Max(0, data->count);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveLast()
    {
        data->count = Mathf.Max(0, data->count - 1);
    }
    public void RemoveAt(int index)
    {
        int last = Length - 1;
        for (int i = index; i < last; ++i)
        {
            this[i] = this[i + 1];
        }
        data->count--;
    }
    public void RemoveElement(int target, System.Func<int, int, bool> conditionFunc)
    {
        for (int i = 0; i < Length; ++i)
        {
            while (conditionFunc(target, this[i]) && i < Length)
            {
                this[i] = this[Length - 1];
                RemoveLast();
            }
        }
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return data->count;
        }
    }
    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return data->capacity;
        }
    }
    public int* unsafePtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return (int*)data->ptr;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (isCreated)
        {
            isCreated = false;
            Allocator alloc = data->allocator;
            UnsafeUtility.Free(data->ptr, alloc);
            UnsafeUtility.Free(data, alloc);
        }
    }
    public ref int this[int id]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            int* ptr = (int*)data->ptr;
            return ref *(ptr + id);
        }
    }

    public ref int this[uint id]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            int* ptr = (int*)data->ptr;
            return ref *(ptr + id);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(int length)
    {
        data->count += length;
        ResizeToCount();
    }
    public void AddRange(int[] array)
    {
        int last = data->count;
        data->count += array.Length;
        ResizeToCount();
        fixed (void* source = &array[0])
        {
            void* dest = unsafePtr + last;
            UnsafeUtility.MemCpy(dest, source, array.Length * sizeof(int));
        }
    }

    public void AddRange(int* array, int length)
    {
        int last = data->count;
        data->count += length;
        ResizeToCount();
        void* dest = unsafePtr + last;
        UnsafeUtility.MemCpy(dest, array, length * sizeof(int));

    }
    public void AddRange(NativeList_Int array)
    {
        int last = data->count;
        data->count += array.Length;
        ResizeToCount();
        void* dest = unsafePtr + last;
        UnsafeUtility.MemCpy(dest, array.unsafePtr, array.Length * sizeof(int));
    }
    public int ConcurrentAdd(int value)
    {
        int last = Interlocked.Increment(ref data->count);
        //Concurrent Resize
        if (last <= data->capacity)
        {
            last--;
            int* ptr = (int*)data->ptr;
            *(ptr + last) = value;
            return last;
        }
        Interlocked.Exchange(ref data->count, data->capacity);
        return -1;
    }
    public int ConcurrentAdd(ref int value)
    {
        int last = Interlocked.Increment(ref data->count);
        //Concurrent Resize
        if (last <= data->capacity)
        {
            last--;
            int* ptr = (int*)data->ptr;
            *(ptr + last) = value;
            return last;
        }
        Interlocked.Exchange(ref data->count, data->capacity);
        return -1;
    }
    public int ConcurrentAdd(int value, object lockerObj)
    {
        int last = Interlocked.Increment(ref data->count);
        //Concurrent Resize
        if (last > data->capacity)
        {
            lock (lockerObj)
            {
                if (last > data->capacity)
                {
                    int newCapacity = data->capacity * 2;
                    void* newPtr = MUnsafeUtility.Malloc<int>(sizeof(int) * newCapacity, data->allocator);
                    UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(int) * data->count);
                    UnsafeUtility.Free(data->ptr, data->allocator);
                    data->ptr = newPtr;
                    data->capacity = newCapacity;
                }
            }
        }
        last--;
        int* ptr = (int*)data->ptr;
        *(ptr + last) = value;
        return last;
    }
    public int ConcurrentAdd(ref int value, object lockerObj)
    {
        int last = Interlocked.Increment(ref data->count);
        //Concurrent Resize
        if (last > data->capacity)
        {
            lock (lockerObj)
            {
                if (last > data->capacity)
                {
                    int newCapacity = data->capacity * 2;
                    void* newPtr = MUnsafeUtility.Malloc<int>(sizeof(int) * newCapacity, data->allocator);
                    UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(int) * data->count);
                    UnsafeUtility.Free(data->ptr, data->allocator);
                    data->ptr = newPtr;
                    data->capacity = newCapacity;
                }
            }
        }
        last--;
        int* ptr = (int*)data->ptr;
        *(ptr + last) = value;
        return last;
    }
    public void Add(int value)
    {
        int last = data->count;
        data->count++;
        Resize();
        int* ptr = (int*)data->ptr;
        *(ptr + last) = value;
    }
    public void Add(ref int value)
    {
        int last = data->count;
        data->count++;
        Resize();
        int* ptr = (int*)data->ptr;
        *(ptr + last) = value;
    }
    public void Remove(int i)
    {
        ref int count = ref data->count;
        if (count == 0) return;
        count--;
        this[i] = this[count];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        data->count = 0;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<int> GetEnumerator()
    {
        return new ListIenumerator_Int(data);
    }
}

public unsafe struct ListIenumerator_Int : IEnumerator<int>
{
    [NativeDisableUnsafePtrRestriction]
    private NativeListData* data;
    private int iteIndex;
    public ListIenumerator_Int(NativeListData* dataPtr)
    {
        data = dataPtr;
        iteIndex = -1;
    }
    object IEnumerator.Current
    {
        get
        {
            return ((int*)data->ptr)[iteIndex];
        }
    }

    public int Current
    {
        get
        {
            return ((int*)data->ptr)[iteIndex];
        }
    }

    public bool MoveNext()
    {
        return (++iteIndex < (data->count));
    }

    public void Reset()
    {
        iteIndex = -1;
    }

    public void Dispose()
    {
    }
}