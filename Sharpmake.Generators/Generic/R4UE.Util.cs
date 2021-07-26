using System;
using System.Linq;

namespace Sharpmake.Generators.Generic
{
    public static class R4UeUtil
    {
        public static bool CheckOptions(this Project.Configuration config, params object[] options)
        {
            var optionsType = typeof(Options);
            var getObjectArgsTypes = new Type[] { typeof(Project.Configuration) };
            var configArg = new object[] { config };

            var getObjectMethod = optionsType.GetMethod("GetObject", getObjectArgsTypes);
            var hasObjectMethod = optionsType.GetMethod("HasOption");
            
            foreach (var option in options)
            {
                var genericHasOption = hasObjectMethod.MakeGenericMethod(option.GetType());
                if (!(bool)genericHasOption.Invoke(null, configArg))
                {
                    continue;
                }
                
                var genericGetObj = getObjectMethod.MakeGenericMethod(option.GetType());
                return genericGetObj.Invoke(null, configArg).Equals(option);
            }

            var genericGetFirstObj = getObjectMethod.MakeGenericMethod(options.First().GetType());
            return genericGetFirstObj.Invoke(null, configArg).Equals(options.First());
        }
    }
}
