using Collections;

namespace Unmanaged.JSON.Unsafe
{
    public unsafe struct UnsafeJSONObject
    {
        private List<JSONProperty> properties;

        private UnsafeJSONObject(List<JSONProperty> properties)
        {
            this.properties = properties;
        }

        public static UnsafeJSONObject* Allocate()
        {
            List<JSONProperty> properties = List<JSONProperty>.Create();
            UnsafeJSONObject* obj = Allocations.Allocate<UnsafeJSONObject>();
            obj[0] = new(properties);
            return obj;
        }

        public static bool IsDisposed(UnsafeJSONObject* obj)
        {
            return obj is null;
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

        public static List<JSONProperty> GetProperties(UnsafeJSONObject* obj)
        {
            Allocations.ThrowIfNull(obj);

            return obj->properties;
        }
    }
}
