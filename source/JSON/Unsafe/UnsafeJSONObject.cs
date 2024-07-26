using Unmanaged.Collections;

namespace Unmanaged.JSON.Unsafe
{
    public unsafe struct UnsafeJSONObject
    {
        private UnmanagedList<JSONProperty> properties;

        private UnsafeJSONObject(UnmanagedList<JSONProperty> properties)
        {
            this.properties = properties;
        }

        public static UnsafeJSONObject* Allocate()
        {
            UnmanagedList<JSONProperty> properties = UnmanagedList<JSONProperty>.Create();
            UnsafeJSONObject* obj = Allocations.Allocate<UnsafeJSONObject>();
            obj[0] = new(properties);
            return obj;
        }

        public static bool IsDisposed(UnsafeJSONObject* obj)
        {
            return Allocations.IsNull(obj) || obj->properties.IsDisposed;
        }

        public static void Free(ref UnsafeJSONObject* obj)
        {
            Allocations.ThrowIfNull(obj);
            uint count = obj->properties.Count;
            for (uint i = 0; i < count; i++)
            {
                JSONProperty property = obj->properties[i];
                property.Dispose();
            }

            obj->properties.Dispose();
            Allocations.Free(ref obj);
        }

        public static UnmanagedList<JSONProperty> GetProperties(UnsafeJSONObject* obj)
        {
            Allocations.ThrowIfNull(obj);
            return obj->properties;
        }
    }
}
