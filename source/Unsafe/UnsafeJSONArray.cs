using Unmanaged.Collections;

namespace Unmanaged.JSON.Array
{
    public unsafe struct UnsafeJSONArray
    {
        private UnmanagedList<JSONProperty> elements;

        private UnsafeJSONArray(UnmanagedList<JSONProperty> elements)
        {
            this.elements = elements;
        }

        public static UnsafeJSONArray* Allocate()
        {
            UnmanagedList<JSONProperty> elements = new();
            UnsafeJSONArray* obj = Allocations.Allocate<UnsafeJSONArray>();
            obj[0] = new(elements);
            return obj;
        }

        public static bool IsDisposed(UnsafeJSONArray* obj)
        {
            return Allocations.IsNull(obj) || obj->elements.IsDisposed;
        }

        public static void Free(ref UnsafeJSONArray* obj)
        {
            Allocations.ThrowIfNull(obj);
            UnmanagedList<JSONProperty> properties = GetElements(obj);
            for (uint i = 0; i < properties.Count; i++)
            {
                JSONProperty property = properties[i];
                property.Dispose();
            }

            obj->elements.Dispose();
            Allocations.Free(ref obj);
        }

        public static uint GetCount(UnsafeJSONArray* obj)
        {
            Allocations.ThrowIfNull(obj);
            return obj->elements.Count;
        }

        public static UnmanagedList<JSONProperty> GetElements(UnsafeJSONArray* obj)
        {
            Allocations.ThrowIfNull(obj);
            return obj->elements;
        }
    }
}