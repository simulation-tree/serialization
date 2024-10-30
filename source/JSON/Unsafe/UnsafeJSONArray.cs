using Collections;

namespace Unmanaged.JSON.Array
{
    public unsafe struct UnsafeJSONArray
    {
        private List<JSONProperty> elements;

        private UnsafeJSONArray(List<JSONProperty> elements)
        {
            this.elements = elements;
        }

        public static UnsafeJSONArray* Allocate()
        {
            List<JSONProperty> elements = List<JSONProperty>.Create();
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
            List<JSONProperty> properties = GetElements(obj);
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

        public static List<JSONProperty> GetElements(UnsafeJSONArray* obj)
        {
            Allocations.ThrowIfNull(obj);
            return obj->elements;
        }
    }
}