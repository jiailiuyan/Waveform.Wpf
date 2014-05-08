using System;

namespace Waveform.Wpf
{
    public static class TypeHelpers
    {
        public static Type TryFindImplementation<T>()
        {
            var t = typeof(T);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (t.IsAssignableFrom(type))
                    {
                        if (type.Name != t.Name)
                        {
                            return type;
                        }
                        else
                        {
                            // skip self
                        }
                    }
                }
            }
            return null;
        }
 
    }
}