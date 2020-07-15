// Copyright (c) 2017 Ubisoft Entertainment
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Sharpmake
{
    // https://johnkoerner.com/csharp/dealing-with-duplicate-attribute-errors-in-net-core/
    public class GeneratedAssemblyConfig
    {
        public bool GenerateAssemblyInfo { get; set; } = false;

        private bool _generateAssemblyConfigurationAttribute = true;
        public bool GenerateAssemblyConfigurationAttribute
        {
            get { return GenerateAssemblyInfo && _generateAssemblyConfigurationAttribute; }
            set { _generateAssemblyConfigurationAttribute = value; }
        }

        private bool _generateAssemblyDescriptionAttribute = true;
        public bool GenerateAssemblyDescriptionAttribute
        {
            get { return GenerateAssemblyInfo && _generateAssemblyDescriptionAttribute; }
            set { _generateAssemblyDescriptionAttribute = value; }
        }

        private bool _generateAssemblyProductAttribute = true;
        public bool GenerateAssemblyProductAttribute
        {
            get { return GenerateAssemblyInfo && _generateAssemblyProductAttribute; }
            set { _generateAssemblyProductAttribute = value; }
        }

        private bool _generateAssemblyTitleAttribute = true;
        public bool GenerateAssemblyTitleAttribute
        {
            get { return GenerateAssemblyInfo && _generateAssemblyTitleAttribute; }
            set { _generateAssemblyTitleAttribute = value; }
        }

        private bool _GenerateAssemblyCompanyAttribute = true;
        public bool GenerateAssemblyCompanyAttribute
        {
            get { return GenerateAssemblyInfo && _GenerateAssemblyCompanyAttribute; }
            set { _GenerateAssemblyCompanyAttribute = value; }
        }

        private bool _generateAssemblyFileVersionAttribute = true;
        public bool GenerateAssemblyFileVersionAttribute
        {
            get { return GenerateAssemblyInfo && _generateAssemblyFileVersionAttribute; }
            set { _generateAssemblyFileVersionAttribute = value; }
        }

        private bool _generateAssemblyVersionAttribute = true;
        public bool GenerateAssemblyVersionAttribute
        {
            get { return GenerateAssemblyInfo && _generateAssemblyVersionAttribute; }
            set { _generateAssemblyVersionAttribute = value; }
        }

        private bool _generateAssemblyInformationalVersionAttribute = true;
        public bool GenerateAssemblyInformationalVersionAttribute
        {
            get { return GenerateAssemblyInfo && _generateAssemblyInformationalVersionAttribute; }
            set { _generateAssemblyInformationalVersionAttribute = value; }
        }
    }

    internal class GeneratedAssemblyConfigTemplate
    {
        private readonly GeneratedAssemblyConfig _config;
        private readonly bool _isNetCoreProjectSchema;
        private readonly string _removeLineTag;

        public GeneratedAssemblyConfigTemplate(GeneratedAssemblyConfig config, bool isNetCoreProjectSchema, string removeLineTag)
        {
            _config = config;
            _isNetCoreProjectSchema = isNetCoreProjectSchema;
            _removeLineTag = removeLineTag;
        }

        public string GenerateAssemblyInfo { get { return (!_isNetCoreProjectSchema || _config.GenerateAssemblyInfo) ? _removeLineTag : "false"; } }
        public string GenerateAssemblyConfigurationAttribute { get { return (!_isNetCoreProjectSchema || !_config.GenerateAssemblyInfo || _config.GenerateAssemblyConfigurationAttribute) ? _removeLineTag : "false"; } }
        public string GenerateAssemblyDescriptionAttribute { get { return (!_isNetCoreProjectSchema || !_config.GenerateAssemblyInfo || _config.GenerateAssemblyDescriptionAttribute) ? _removeLineTag : "false"; } }
        public string GenerateAssemblyProductAttribute { get { return (!_isNetCoreProjectSchema || !_config.GenerateAssemblyInfo || _config.GenerateAssemblyProductAttribute) ? _removeLineTag : "false"; } }
        public string GenerateAssemblyTitleAttribute { get { return (!_isNetCoreProjectSchema || !_config.GenerateAssemblyInfo || _config.GenerateAssemblyTitleAttribute) ? _removeLineTag : "false"; } }
        public string GenerateAssemblyCompanyAttribute { get { return (!_isNetCoreProjectSchema || !_config.GenerateAssemblyInfo || _config.GenerateAssemblyCompanyAttribute) ? _removeLineTag : "false"; } }
        public string GenerateAssemblyFileVersionAttribute { get { return (!_isNetCoreProjectSchema || !_config.GenerateAssemblyInfo || _config.GenerateAssemblyFileVersionAttribute) ? _removeLineTag : "false"; } }
        public string GenerateAssemblyVersionAttribute { get { return (!_isNetCoreProjectSchema || !_config.GenerateAssemblyInfo || _config.GenerateAssemblyVersionAttribute) ? _removeLineTag : "false"; } }
        public string GenerateAssemblyInformationalVersionAttribute { get { return (!_isNetCoreProjectSchema || !_config.GenerateAssemblyInfo || _config.GenerateAssemblyInformationalVersionAttribute) ? _removeLineTag : "false"; } }
    }
}
