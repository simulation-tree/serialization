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
            List<JSONProperty> elements = new(4);
            UnsafeJSONArray* obj = Allocations.Allocate<UnsafeJSONArray>();
            obj[0] = new(elements);
            return obj;
        }

        public static bool IsDisposed(UnsafeJSONArray* array)
        {
            return array is null;
        }

        public static void Free(ref UnsafeJSONArray* array)
        {
            Allocations.ThrowIfNull(array);

            List<JSONProperty> properties = GetElements(array);
            for (uint i = 0; i < properties.Count; i++)
            {
                JSONProperty property = properties[i];
                property.Dispose();
            }

            array->elements.Dispose();
            Allocations.Free(ref array);
        }

        public static uint GetCount(UnsafeJSONArray* array)
        {
            Allocations.ThrowIfNull(array);

            return array->elements.Count;
        }

        public static List<JSONProperty> GetElements(UnsafeJSONArray* array)
        {
            Allocations.ThrowIfNull(array);

            return array->elements;
        }
    }
}