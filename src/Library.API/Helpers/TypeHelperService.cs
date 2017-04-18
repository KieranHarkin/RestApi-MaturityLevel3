using System.Reflection;

namespace Library.API.Helpers
{
    public class TypeHelperService : ITypeHelperService
    {
        public bool TypeHasProperties<T>(string fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            var fieldsAfterSplit = fields.Split(',');

            // check if the requested fields exist on source
            foreach (var field in fieldsAfterSplit)
            {
                var propertyName = field.Trim();

                // use reflection to get the property on the soource object
                // we need to include public and instance, b/c specifying a binding flag
                // overwrites the already existing binding flags.
                var propertyInfo = typeof(T)
                    .GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo == null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
