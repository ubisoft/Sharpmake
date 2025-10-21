// Copyright (c) Ubisoft. All Rights Reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using StartActionSetting = Sharpmake.Project.Configuration.CsprojUserFileSettings.StartActionSetting;

namespace Sharpmake.Generators.VisualStudio
{
    public partial class CSproj : IProjectGenerator
    {
        private const string TTExtension = ".tt";

        internal class TargetFramework : IEquatable<TargetFramework>
        {
            public readonly DotNetFramework DotNetFramework;
            public readonly DotNetOS DotNetOSVersion;
            public readonly string DotNetOSVersionSuffix = string.Empty;
            public TargetFramework(DotNetFramework dotNetFramework, DotNetOS dotNetOSVersion = DotNetOS.Default, string dotNetOSVersionSuffix = "")
            {
                DotNetFramework = dotNetFramework;
                DotNetOSVersion = dotNetOSVersion;
                DotNetOSVersionSuffix = dotNetOSVersionSuffix;
            }

            public override string ToString()
            {
                return GetTargetFrameworksString(this);
            }

            #region IEquatable
            public override bool Equals(object obj)
            {
                TargetFramework other = obj as TargetFramework;
                if (other != null)
                {
                    return Equals(other);
                }
                else
                {
                    return false;
                }
            }

            public bool Equals(TargetFramework other)
            {
                return DotNetFramework == other.DotNetFramework
                    && DotNetOSVersion == other.DotNetOSVersion
                    && DotNetOSVersionSuffix == other.DotNetOSVersionSuffix;
            }

            public override int GetHashCode()
            {
                int hash = DotNetFramework.GetHashCode() * 5
                    + (DotNetOSVersion.GetHashCode() * 7)
                    + (DotNetOSVersionSuffix.GetHashCode() * 11);
                return hash;
            }
            #endregion
        }

        internal interface IResolvable
        {
            string Resolve(Resolver resolver);
        }

        internal interface IResolvableCondition : IResolvable
        {
            string ResolveCondition(Resolver resolver);
        }

        internal class ItemGroups
        {
            internal ItemGroupConditional<TargetFrameworksCondition<Reference>> References = new ItemGroupConditional<TargetFrameworksCondition<Reference>>();
            internal ItemGroupConditional<TargetFrameworksCondition<FrameworkReference>> FrameworkReferences = new ItemGroupConditional<TargetFrameworksCondition<FrameworkReference>>();
            internal ItemGroup<Service> Services = new ItemGroup<Service>();
            internal ItemGroup<Compile> Compiles = new ItemGroup<Compile>();
            internal ItemGroup<ProjectReference> ProjectReferences = new ItemGroup<ProjectReference>();
            internal ItemGroup<BootstrapperPackage> BootstrapperPackages = new ItemGroup<BootstrapperPackage>();
            internal ItemGroup<FileAssociationItem> FileAssociationItems = new ItemGroup<FileAssociationItem>();
            internal ItemGroup<PublishFile> PublishFiles = new ItemGroup<PublishFile>();
            internal ItemGroup<EmbeddedResource> EmbeddedResources = new ItemGroup<EmbeddedResource>();
            internal ItemGroup<Page> Pages = new ItemGroup<Page>();
            internal ItemGroup<Resource> Resources = new ItemGroup<Resource>();
            internal ItemGroup<None> Nones = new ItemGroup<None>();
            internal ItemGroup<Content> Contents = new ItemGroup<Content>();
            internal ItemGroup<Vsct> Vscts = new ItemGroup<Vsct>();
            internal ItemGroup<VsdConfigXml> VsdConfigXmls = new ItemGroup<VsdConfigXml>();
            internal ItemGroup<WebReference> WebReferences = new ItemGroup<WebReference>();
            internal ItemGroup<WebReferenceUrl> WebReferenceUrls = new ItemGroup<WebReferenceUrl>();
            internal ItemGroup<ComReference> ComReferences = new ItemGroup<ComReference>();
            internal ItemGroup<EntityDeploy> EntityDeploys = new ItemGroup<EntityDeploy>();
            internal ItemGroup<WCFMetadataStorage> WCFMetadataStorages = new ItemGroup<WCFMetadataStorage>();
            internal ItemGroup<SplashScreen> AppSplashScreen = new ItemGroup<SplashScreen>();
            internal ItemGroupConditional<TargetFrameworksCondition<ItemTemplate>> PackageReferences = new ItemGroupConditional<TargetFrameworksCondition<ItemTemplate>>();
            internal ItemGroup<Analyzer> Analyzers = new ItemGroup<Analyzer>();
            internal ItemGroup<VSIXSourceItem> VSIXSourceItems = new ItemGroup<VSIXSourceItem>();
            internal ItemGroup<FolderInclude> FolderIncludes = new ItemGroup<FolderInclude>();
            internal ItemGroup<Protobuf> Protobufs = new ItemGroup<Protobuf>();

            internal string Resolve(Resolver resolver)
            {
                var writer = new StringWriter();
                writer.Write(References.Resolve(resolver));
                writer.Write(FrameworkReferences.Resolve(resolver));
                writer.Write(Services.Resolve(resolver));
                writer.Write(Compiles.Resolve(resolver));
                writer.Write(Vscts.Resolve(resolver));
                writer.Write(VsdConfigXmls.Resolve(resolver));
                writer.Write(Nones.Resolve(resolver));
                writer.Write(EntityDeploys.Resolve(resolver));
                writer.Write(Pages.Resolve(resolver));
                writer.Write(Resources.Resolve(resolver));
                writer.Write(EmbeddedResources.Resolve(resolver));
                writer.Write(BootstrapperPackages.Resolve(resolver));
                writer.Write(FileAssociationItems.Resolve(resolver));
                writer.Write(PublishFiles.Resolve(resolver));
                writer.Write(ProjectReferences.Resolve(resolver));
                writer.Write(WebReferences.Resolve(resolver));
                writer.Write(WebReferenceUrls.Resolve(resolver));
                writer.Write(ComReferences.Resolve(resolver));
                writer.Write(Contents.Resolve(resolver));
                writer.Write(AppSplashScreen.Resolve(resolver));
                writer.Write(PackageReferences.Resolve(resolver));
                writer.Write(Analyzers.Resolve(resolver));
                writer.Write(VSIXSourceItems.Resolve(resolver));
                writer.Write(FolderIncludes.Resolve(resolver));
                writer.Write(WCFMetadataStorages.Resolve(resolver));
                writer.Write(Protobufs.Resolve(resolver));

                return writer.ToString();
            }

            internal class ItemGroup<T> : UniqueList<T>, IResolvable where T : IResolvable
            {
                public string Resolve(Resolver resolver)
                {
                    if (Count <= 0)
                        return string.Empty;

                    var writer = new StringWriter();
                    writer.Write(Template.ItemGroups.ItemGroupBegin);
                    var sortedValues = SortedValues;
                    foreach (T elem in sortedValues)
                    {
                        writer.Write(elem.Resolve(resolver));
                    }
                    writer.Write(Template.ItemGroups.ItemGroupEnd);
                    return writer.ToString();
                }
            }

            internal class ItemGroupConditional<T> : UniqueList<T>, IResolvable where T : IResolvableCondition
            {
                public T AlwaysTrueElement;

                public string Resolve(Resolver resolver)
                {
                    if (Count <= 0)
                        return string.Empty;

                    var conditionalItemGroups = new Dictionary<string, List<string>>();
                    foreach (T elem in Values)
                    {
                        var resolvedElemCondition = elem.ResolveCondition(resolver);
                        var resolvedElem = elem.Resolve(resolver);
                        if (conditionalItemGroups.ContainsKey(resolvedElemCondition))
                            conditionalItemGroups[resolvedElemCondition].Add(resolvedElem);
                        else
                            conditionalItemGroups.Add(resolvedElemCondition, new List<string> { resolvedElem });
                    }

                    // No ItemGroup, skip
                    if (!conditionalItemGroups.Any())
                        return string.Empty;

                    var writer = new StringWriter();
                    var resolvedAlwaysTrueCondition = AlwaysTrueElement?.ResolveCondition(resolver);
                    foreach (var conditionalItemGroup in conditionalItemGroups.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        // No element for this ItemGroup, skip
                        if (!conditionalItemGroup.Value.Any())
                            continue;

                        if (resolvedAlwaysTrueCondition != null && resolvedAlwaysTrueCondition == conditionalItemGroup.Key)
                        {
                            writer.Write(Template.ItemGroups.ItemGroupBegin);
                        }
                        else
                        {
                            using (resolver.NewScopedParameter("itemGroupCondition", conditionalItemGroup.Key))
                                writer.Write(resolver.Resolve(Template.ItemGroups.ItemGroupConditionalBegin));
                        }

                        foreach (var elem in conditionalItemGroup.Value.OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
                            writer.Write(elem);

                        writer.Write(Template.ItemGroups.ItemGroupEnd);
                    }
                    return writer.ToString();
                }
            }

            internal class TargetFrameworksCondition<T> : UniqueList<T>, IResolvableCondition where T : IResolvable
            {
                public List<TargetFramework> TargetFrameworks;

                public string ResolveCondition(Resolver resolver)
                {
                    return string.Join(" OR ", TargetFrameworks.Select(targetFramework =>
                    {
                        using (resolver.NewScopedParameter("targetFramework", GetTargetFrameworksString(targetFramework)))
                        {
                            return resolver.Resolve(Template.ItemGroups
                                .ItemGroupTargetFrameworkCondition);
                        }
                    }));
                }

                public string Resolve(Resolver resolver)
                {
                    if (Count <= 0)
                        return string.Empty;
                    var writer = new StringWriter();
                    var sortedValues = SortedValues;
                    foreach (T elem in sortedValues)
                    {
                        writer.Write(elem.Resolve(resolver));
                    }
                    return writer.ToString();
                }
            }

            internal class ItemGroupItem : IComparable<ItemGroupItem>, IEquatable<ItemGroupItem>
            {
                public string Include;

                // This property is used to decide if this object is a Link
                // If LinkFolder is null, this item is in the project folder and is not a link
                // If LinkFolder is empty, this item is in the project's SourceRootPath or RootPath folder
                //      which are outside of the Project folder and is a link.
                // If LinkedFolder is a file path, it's a link. 
                public string LinkFolder = null;

                private bool IsLink { get { return LinkFolder != null; } }

                private string Link
                {
                    get
                    {
                        string filename = Path.GetFileName(Include);
                        return Path.Combine(LinkFolder, filename);
                    }
                }

                protected Resolver.ScopedParameter LinkParameter(Resolver resolver)
                {
                    return IsLink ? resolver.NewScopedParameter("link", Link) : null;
                }

                protected void AddLinkIfNeeded(StringWriter writer)
                {
                    if (IsLink)
                        writer.Write(Template.ItemGroups.Link);
                }

                #region Equality members

                public bool Equals(ItemGroupItem other)
                {
                    if (ReferenceEquals(null, other))
                        return false;
                    if (ReferenceEquals(this, other))
                        return true;
                    return string.Equals(Include, other.Include);
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj))
                        return false;
                    if (ReferenceEquals(this, obj))
                        return true;
                    if (obj.GetType() != GetType())
                        return false;
                    return Equals((ItemGroupItem)obj);
                }

                public override int GetHashCode()
                {
                    return (Include != null ? Include.GetHashCode() : 0);
                }

                public static bool operator ==(ItemGroupItem left, ItemGroupItem right)
                {
                    return Equals(left, right);
                }

                public static bool operator !=(ItemGroupItem left, ItemGroupItem right)
                {
                    return !Equals(left, right);
                }

                #endregion

                public int CompareTo(ItemGroupItem other)
                {
                    return string.CompareOrdinal(Include, other.Include);
                }
            }

            internal class WebReferenceUrl : ItemGroupItem, IResolvable
            {
                public string UrlBehavior;
                public string RelPath;
                public string UpdateFromURL;
                public string ServiceLocationURL;
                public string CachedDynamicPropName;
                public string CachedAppSettingsObjectName;
                public string CachedSettingsPropName;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("urlBehavior", UrlBehavior))
                    using (resolver.NewScopedParameter("relPath", RelPath))
                    using (resolver.NewScopedParameter("updateFromURL", UpdateFromURL))
                    using (resolver.NewScopedParameter("serviceLocationURL", ServiceLocationURL))
                    using (resolver.NewScopedParameter("cachedDynamicPropName", CachedDynamicPropName))
                    using (resolver.NewScopedParameter("cachedAppSettingsObjectName", CachedAppSettingsObjectName))
                    using (resolver.NewScopedParameter("cachedSettingsPropName", CachedSettingsPropName))
                    {
                        var writer = new StringWriter();

                        writer.Write(Template.WebReferenceUrlBegin);
                        writer.Write(Template.UrlBehavior);
                        writer.Write(Template.RelPath);
                        writer.Write(Template.UpdateFromURL);
                        if (CachedDynamicPropName != null)
                            writer.Write(Template.CachedDynamicPropName);
                        writer.Write(Template.CachedAppSettingsObjectName);
                        writer.Write(Template.CachedSettingsPropName);
                        writer.Write(Template.WebReferenceUrlEnd);

                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class ComReference : ItemGroupItem, IResolvable
            {
                public Guid Guid;
                public int VersionMajor;
                public int VersionMinor;
                public int Lcid;
                public string WrapperTool;
                public bool? Private;
                public bool? EmbedInteropTypes;

                public string Resolve(Resolver resolver)
                {
                    string privateValue = RemoveLineTag;
                    if (Private.HasValue)
                        privateValue = Private.ToString().ToLower();

                    string embedInteropTypesValue = RemoveLineTag;
                    if (EmbedInteropTypes.HasValue)
                        embedInteropTypesValue = EmbedInteropTypes.ToString();

                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("guid", Guid))
                    using (resolver.NewScopedParameter("versionMajor", VersionMajor))
                    using (resolver.NewScopedParameter("versionMinor", VersionMinor))
                    using (resolver.NewScopedParameter("lcid", Lcid))
                    using (resolver.NewScopedParameter("wrapperTool", WrapperTool))
                    using (resolver.NewScopedParameter("private", privateValue))
                    using (resolver.NewScopedParameter("EmbedInteropTypes", embedInteropTypesValue))
                    {
                        var writer = new StringWriter();

                        writer.Write(Template.COMReference);

                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class WebReference : ItemGroupItem, IResolvable
            {
                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    {
                        return resolver.Resolve(Template.ItemGroups.SimpleWebReference);
                    }
                }
            }

            internal class FolderInclude : ItemGroupItem, IResolvable
            {
                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("folder", Include))
                    {
                        return resolver.Resolve(Template.ItemGroups.Folder);
                    }
                }
            }

            internal class Reference : ItemGroupItem, IResolvable
            {
                public bool? SpecificVersion;
                public string HintPath;
                public bool? Private;
                public bool? EmbedInteropTypes;

                public string Resolve(Resolver resolver)
                {
                    var writer = new StringWriter();

                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("specificVersion", SpecificVersion))
                    using (resolver.NewScopedParameter("hintPath", HintPath))
                    using (resolver.NewScopedParameter("private", Private))
                    using (resolver.NewScopedParameter("embedInteropTypes", EmbedInteropTypes))
                    {
                        if (SpecificVersion == null && string.IsNullOrEmpty(HintPath) && Private == null && EmbedInteropTypes == null)
                            writer.Write(Template.ItemGroups.SimpleReference);
                        else
                        {
                            writer.Write(Template.ItemGroups.ReferenceBegin);
                            if (SpecificVersion.HasValue)
                                writer.Write(Template.ItemGroups.SpecificVersion);
                            if (!string.IsNullOrEmpty(HintPath))
                                writer.Write(Template.ItemGroups.HintPath);
                            if (Private.HasValue)
                                writer.Write(Template.ItemGroups.Private);
                            if (EmbedInteropTypes.HasValue)
                                writer.Write(Template.ItemGroups.EmbedInteropTypes);

                            writer.Write(Template.ItemGroups.ReferenceEnd);
                        }
                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class SplashScreen : ItemGroupItem, IResolvable
            {
                public string Resolve(Resolver resolver)
                {
                    using (var writer = new StringWriter())
                    {
                        using (resolver.NewScopedParameter("include", Include))
                        {
                            if (!string.IsNullOrWhiteSpace(Include))
                                writer.Write(Template.ItemGroups.SplashScreen);

                            return resolver.Resolve(writer.ToString());
                        }
                    }
                }
            }

            internal class Content : ItemGroupItem, IResolvable
            {
                public string Generator;
                public string LastGenOutput;
                public CopyToOutputDirectory? CopyToOutputDirectory;
                public string IncludeInVsix;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("copyToOutputDirectory", CopyToOutputDirectory))
                    using (resolver.NewScopedParameter("includeInVsix", IncludeInVsix))
                    using (resolver.NewScopedParameter("generator", Generator))
                    using (resolver.NewScopedParameter("lastGenOutput", LastGenOutput))
                    using (LinkParameter(resolver))
                    {
                        var builder = new StringBuilder();
                        var writer = new StringWriter(builder);

                        if (Generator != null)
                            writer.Write(Template.ItemGroups.Generator);
                        if (LastGenOutput != null)
                            writer.Write(Template.ItemGroups.LastGenOutput);
                        if (CopyToOutputDirectory != null)
                            writer.Write(Template.ItemGroups.CopyToOutputDirectory);
                        if (IncludeInVsix != null)
                            writer.Write(Template.ItemGroups.IncludeInVsix);
                        AddLinkIfNeeded(writer);

                        if (builder.Length == 0)
                            return resolver.Resolve(Template.ItemGroups.ContentSimple);

                        builder.Insert(0, Template.ItemGroups.ContentBegin);
                        writer.Write(Template.ItemGroups.ContentEnd);
                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class Analyzer : ItemGroupItem, IResolvable
            {
                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (LinkParameter(resolver))
                    {
                        var builder = new StringBuilder();
                        var writer = new StringWriter(builder);

                        writer.Write(Template.ItemGroups.Analyzer);

                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class Vsct : ItemGroupItem, IResolvable
            {
                public string ResourceName = "Menus.ctmenu";
                public string SubType = "Designer";

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("resourceName", ResourceName))
                    using (resolver.NewScopedParameter("subType", SubType))
                    {
                        var writer = new StringWriter();
                        writer.Write(Template.ItemGroups.VsctCompileBegin);
                        writer.Write(Template.ItemGroups.ResourceName);
                        writer.Write(Template.ItemGroups.SubType);
                        writer.Write(Template.ItemGroups.VsctCompileEnd);
                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class VsdConfigXml : ItemGroupItem, IResolvable
            {
                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    {
                        var writer = new StringWriter();
                        writer.Write(Template.ItemGroups.VsdConfigXmlSimple);
                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class None : ItemGroupItem, IResolvable
            {
                public string Generator;
                public string LastGenOutput;
                public string DependentUpon = null;
                public CopyToOutputDirectory? CopyToOutputDirectory;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("lastGenOutput", LastGenOutput))
                    using (resolver.NewScopedParameter("generator", Generator))
                    using (resolver.NewScopedParameter("dependentUpon", DependentUpon))
                    using (resolver.NewScopedParameter("copyToOutputDirectory", CopyToOutputDirectory))
                    using (LinkParameter(resolver))
                    {
                        var builder = new StringBuilder();
                        var writer = new StringWriter(builder);

                        if (Generator != null)
                            writer.Write(Template.ItemGroups.Generator);
                        if (LastGenOutput != null)
                            writer.Write(Template.ItemGroups.LastGenOutput);
                        if (DependentUpon != null)
                            writer.Write(Template.ItemGroups.DependentUpon);
                        if (CopyToOutputDirectory != null)
                            writer.Write(Template.ItemGroups.CopyToOutputDirectory);
                        AddLinkIfNeeded(writer);

                        if (builder.Length == 0)
                            return resolver.Resolve(Template.ItemGroups.SimpleNone);

                        builder.Insert(0, Template.ItemGroups.NoneItemGroupBegin);
                        writer.Write(Template.ItemGroups.NoneItemGroupEnd);
                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class Service : ItemGroupItem, IResolvable
            {
                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    {
                        return resolver.Resolve(Template.ItemGroups.Service);
                    }
                }
            }

            internal class Compile : ItemGroupItem, IResolvable
            {
                public bool? AutoGen;
                public bool? DesignTime = null;
                public bool? DesignTimeSharedInput = null;
                public string DependentUpon;
                public string SubType = null;
                public string Exclude;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("exclude", Exclude ?? string.Empty))
                    using (resolver.NewScopedParameter("autoGen", AutoGen))
                    using (resolver.NewScopedParameter("designTime", DesignTime))
                    using (resolver.NewScopedParameter("designTimeSharedInput", DesignTimeSharedInput))
                    using (resolver.NewScopedParameter("dependentUpon", DependentUpon))
                    using (resolver.NewScopedParameter("subType", SubType))
                    using (LinkParameter(resolver))
                    {
                        var builder = new StringBuilder();
                        var writer = new StringWriter(builder);

                        if (AutoGen.HasValue)
                            writer.Write(Template.ItemGroups.AutoGen);
                        if (DesignTime.HasValue)
                            writer.Write(Template.ItemGroups.DesignTime);
                        if (DesignTimeSharedInput.HasValue)
                            writer.Write(Template.ItemGroups.DesignTimeSharedInput);
                        if (DependentUpon != null)
                            writer.Write(Template.ItemGroups.DependentUpon);
                        if (SubType != null)
                            writer.Write(Template.ItemGroups.SubType);
                        AddLinkIfNeeded(writer);

                        if (builder.Length == 0)
                            return resolver.Resolve(string.IsNullOrEmpty(Exclude) ? Template.ItemGroups.SimpleCompile : Template.ItemGroups.SimpleCompileWithExclude);

                        builder.Insert(0, string.IsNullOrEmpty(Exclude) ? Template.ItemGroups.CompileBegin : Template.ItemGroups.CompileBeginWithExclude);
                        writer.Write(Template.ItemGroups.CompileEnd);
                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class ProjectReference : ItemGroupItem, IResolvable
            {
                public Guid Project;
                public string Name;
                public bool Private = false;
                public bool? ReferenceOutputAssembly = true;
                public string IncludeOutputGroupsInVSIX = null;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("projectGUID", Project.ToString("B")))
                    using (resolver.NewScopedParameter("projectRefName", Name))
                    using (resolver.NewScopedParameter("private", Private.ToString().ToLower()))
                    using (resolver.NewScopedParameter("ReferenceOutputAssembly", ReferenceOutputAssembly))
                    using (resolver.NewScopedParameter("IncludeOutputGroupsInVSIX", IncludeOutputGroupsInVSIX))
                    {
                        var writer = new StringWriter();

                        writer.Write(Template.ItemGroups.ProjectReferenceBegin);
                        writer.Write(Template.ItemGroups.ProjectGUID);
                        writer.Write(Template.ItemGroups.ProjectRefName);
                        if (!Private)
                            writer.Write(Template.ItemGroups.Private);
                        if (ReferenceOutputAssembly.HasValue)
                            writer.Write(Template.ItemGroups.ReferenceOutputAssembly);
                        if (IncludeOutputGroupsInVSIX != null)
                            writer.Write(Template.ItemGroups.IncludeOutputGroupsInVSIX);
                        writer.Write(Template.ItemGroups.ProjectReferenceEnd);
                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class FrameworkReference : ItemGroupItem, IResolvable
            {
                /// <inheritdoc />
                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    {
                        return resolver.Resolve(Template.ItemGroups.FrameworkReference);
                    }
                }
            }

            internal class ItemTemplate : IResolvable, IComparable<ItemTemplate>, IEquatable<ItemTemplate>
            {
                private readonly string _template;

                public ItemTemplate(string template)
                {
                    _template = template;
                }

                public string Resolve(Resolver resolver)
                {
                    return resolver.Resolve(_template);
                }

                public int CompareTo(ItemTemplate other)
                {
                    if (ReferenceEquals(this, other))
                        return 0;
                    if (ReferenceEquals(null, other))
                        return 1;
                    return string.Compare(_template, other._template, StringComparison.OrdinalIgnoreCase);
                }

                public bool Equals(ItemTemplate other)
                {
                    if (ReferenceEquals(null, other))
                        return false;
                    if (ReferenceEquals(this, other))
                        return true;
                    return string.Equals(_template, other._template, StringComparison.OrdinalIgnoreCase);
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj))
                        return false;
                    if (ReferenceEquals(this, obj))
                        return true;
                    if (obj.GetType() != this.GetType())
                        return false;
                    return Equals((ItemTemplate)obj);
                }

                public override int GetHashCode()
                {
                    return StringComparer.OrdinalIgnoreCase.GetHashCode(_template);
                }
            }

            //Available field which could be supported
            //https://msdn.microsoft.com/en-us/library/ms164294.aspx
            internal class BootstrapperPackage : ItemGroups.ItemGroupItem, IResolvable
            {
                public bool Visible = true;
                public string ProductName = null;
                public bool Install = true;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("visible", Visible))
                    using (resolver.NewScopedParameter("productName", ProductName))
                    using (resolver.NewScopedParameter("install", Install))
                    {
                        return resolver.Resolve(Template.ItemGroups.BootstrapperPackage);
                    }
                }
            }

            internal class FileAssociationItem : ItemGroupItem, IResolvable
            {
                public bool? Visible;
                public string Description;
                public string Progid;
                public string DefaultIcon;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("visible", Visible))
                    using (resolver.NewScopedParameter("description", Description))
                    using (resolver.NewScopedParameter("progid", Progid))
                    using (resolver.NewScopedParameter("defaultIcon", DefaultIcon))
                    using (LinkParameter(resolver))
                    {
                        return resolver.Resolve(Template.ItemGroups.FileAssociationItem);
                    }
                }
            }

            internal class PublishFile : ItemGroupItem, IResolvable
            {
                public bool? Visible;
                public string Group;
                public PublishState PublishState;
                public bool IncludeHash;
                public FileType FileType;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("visible", Visible))
                    using (resolver.NewScopedParameter("group", Group))
                    using (resolver.NewScopedParameter("publishState", PublishState))
                    using (resolver.NewScopedParameter("includeHash", IncludeHash))
                    using (resolver.NewScopedParameter("fileType", FileType))
                    using (LinkParameter(resolver))
                    {
                        return resolver.Resolve(Template.ItemGroups.PublishFile);
                    }
                }
            }

            internal class EmbeddedResource : ItemGroups.ItemGroupItem, IResolvable
            {
                public string DependUpon = null;
                public string MergeWithCto = null;
                public string Generator = null;
                public string LastGenOutput = null;
                public string SubType = "Designer";
                public CopyToOutputDirectory? CopyToOutputDirectory = null;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("generator", Generator))
                    using (resolver.NewScopedParameter("lastGenOutput", LastGenOutput))
                    using (resolver.NewScopedParameter("subType", SubType))
                    using (resolver.NewScopedParameter("copyToOutputDirectory", CopyToOutputDirectory))
                    using (resolver.NewScopedParameter("dependentUpon", DependUpon))
                    using (resolver.NewScopedParameter("mergeWithCto", MergeWithCto))
                    using (LinkParameter(resolver))
                    {
                        var builder = new StringBuilder();
                        var writer = new StringWriter(builder);

                        if (!string.IsNullOrEmpty(Generator))
                            writer.Write(Template.ItemGroups.Generator);
                        if (!string.IsNullOrWhiteSpace(LastGenOutput))
                            writer.Write(Template.ItemGroups.LastGenOutput);
                        if (!string.IsNullOrWhiteSpace(SubType))
                            writer.Write(Template.ItemGroups.SubType);
                        if (CopyToOutputDirectory != null)
                            writer.Write(Template.ItemGroups.CopyToOutputDirectory);
                        if (!string.IsNullOrWhiteSpace(DependUpon))
                            writer.Write(Template.ItemGroups.DependentUpon);
                        if (!string.IsNullOrWhiteSpace(MergeWithCto))
                            writer.Write(Template.ItemGroups.MergeWithCto);
                        AddLinkIfNeeded(writer);

                        if (builder.Length == 0)
                            return resolver.Resolve(Template.ItemGroups.SimpleEmbeddedResource);

                        builder.Insert(0, Template.ItemGroups.EmbeddedResourceBegin);
                        builder.Append(Template.ItemGroups.EmbeddedResourceEnd);

                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class Page : ItemGroups.ItemGroupItem, IResolvable
            {
                public bool? AutoGen;
                public string DependentUpon = null;
                public string Generator = "MSBuild:Compile";
                public string SubType = "Designer";
                public bool IsApplicationDefinition = false;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("generator", Generator))
                    using (resolver.NewScopedParameter("subType", SubType))
                    using (resolver.NewScopedParameter("autoGen", AutoGen))
                    using (resolver.NewScopedParameter("dependentUpon", DependentUpon))
                    using (LinkParameter(resolver))
                    {
                        var writer = new StringWriter();
                        if (!IsApplicationDefinition)
                        {
                            writer.Write(Template.ItemGroups.PageBegin);
                            if (AutoGen != null)
                                writer.Write(Template.ItemGroups.AutoGen);
                            if (DependentUpon != null)
                                writer.Write(Template.ItemGroups.DependentUpon);
                            writer.Write(Template.ItemGroups.Generator);
                            writer.Write(Template.ItemGroups.SubType);
                            AddLinkIfNeeded(writer);
                            writer.Write(Template.ItemGroups.PageEnd);
                        }
                        else
                        {
                            writer.Write(Template.ItemGroups.ApplicationDefinitionBegin);
                            if (AutoGen != null)
                                writer.Write(Template.ItemGroups.AutoGen);
                            if (DependentUpon != null)
                                writer.Write(Template.ItemGroups.DependentUpon);
                            writer.Write(Template.ItemGroups.Generator);
                            writer.Write(Template.ItemGroups.SubType);
                            AddLinkIfNeeded(writer);
                            writer.Write(Template.ItemGroups.ApplicationDefinitionEnd);
                        }

                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class Resource : ItemGroups.ItemGroupItem, IResolvable
            {
                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("resource", Include))
                    using (resolver.NewScopedParameter("include", Include))
                    using (LinkParameter(resolver))
                    {
                        var builder = new StringBuilder();
                        var writer = new StringWriter(builder);

                        AddLinkIfNeeded(writer);

                        if (builder.Length == 0)
                            return resolver.Resolve(Template.ItemGroups.SimpleResource);

                        builder.Insert(0, Template.ItemGroups.ResourceBegin);
                        builder.Append(Template.ItemGroups.ResourceEnd);
                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class EntityDeploy : ItemGroups.ItemGroupItem, IResolvable
            {
                public string Generator = "EntityModelCodeGenerator";
                public string LastGenOutput = null;

                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    using (resolver.NewScopedParameter("generator", Generator))
                    using (resolver.NewScopedParameter("lastGenOutput", LastGenOutput))
                    {
                        var writer = new StringWriter();
                        writer.Write(Template.ItemGroups.EntityDeployBegin);
                        writer.Write(Template.ItemGroups.Generator);
                        writer.Write(Template.ItemGroups.LastGenOutput);
                        writer.Write(Template.ItemGroups.EntityDeployEnd);

                        return resolver.Resolve(writer.ToString());
                    }
                }
            }

            internal class WCFMetadataStorage : ItemGroups.ItemGroupItem, IResolvable
            {
                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("storage", Include))
                        return resolver.Resolve(Template.ItemGroups.WCFMetadataStorage);
                }
            }

            internal class VSIXSourceItem : ItemGroups.ItemGroupItem, IResolvable
            {
                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("vsixSourceItem", Include))
                        return resolver.Resolve(Template.ItemGroups.VSIXSourceItem);
                }
            }

            internal class Protobuf : ItemGroupItem, IResolvable
            {
                /// <inheritdoc />
                public string Resolve(Resolver resolver)
                {
                    using (resolver.NewScopedParameter("include", Include))
                    {
                        return resolver.Resolve(Template.ItemGroups.Protobuf);
                    }
                }
            }

            private static void AddTargetFrameworksCondition<T>(ItemGroupConditional<TargetFrameworksCondition<T>> itemGroupConditional, TargetFramework targetFramework, T elem) where T : IResolvable
            {
                if (itemGroupConditional.Any(it => it.Contains(elem)))
                {
                    foreach (var itemGroup in itemGroupConditional.Where(it => it.Contains(elem)))
                    {
                        if (!itemGroup.TargetFrameworks.Contains(targetFramework))
                            itemGroup.TargetFrameworks.Add(targetFramework);
                    }
                }
                else
                {
                    var newItemGroup = new TargetFrameworksCondition<T>
                    {
                        TargetFrameworks = new List<TargetFramework> { targetFramework },
                    };
                    newItemGroup.Add(elem);
                    itemGroupConditional.Add(newItemGroup);
                }
            }

            public void SetTargetFrameworks(List<TargetFramework> projectFrameworks)
            {
                References.AlwaysTrueElement = new TargetFrameworksCondition<Reference>
                {
                    TargetFrameworks = projectFrameworks
                };
                FrameworkReferences.AlwaysTrueElement = new TargetFrameworksCondition<FrameworkReference>()
                {
                    TargetFrameworks = projectFrameworks
                };
                PackageReferences.AlwaysTrueElement = new TargetFrameworksCondition<ItemTemplate>
                {
                    TargetFrameworks = projectFrameworks
                };
            }

            [Obsolete("Use AddReference(TargetFramework, Reference) instead")]
            public void AddReference(DotNetFramework dotNetFramework, Reference reference)
            {
                AddReference(new TargetFramework(dotNetFramework), reference);
            }

            public void AddReference(TargetFramework targetFramework, Reference reference)
            {
                AddTargetFrameworksCondition(References, targetFramework, reference);
            }

            public void AddPackageReference(TargetFramework targetFramework, ItemTemplate itemTemplate)
            {
                AddTargetFrameworksCondition(PackageReferences, targetFramework, itemTemplate);
            }

            public void AddFrameworkReference(FrameworkReference frameworkReference, TargetFramework targetFramework)
            {
                AddTargetFrameworksCondition(FrameworkReferences, targetFramework, frameworkReference);
            }
        }

        internal class Choose : IResolvable
        {
            public Dictionary<string, List<IResolvable>> Choices = new Dictionary<string, List<IResolvable>>();

            public string Resolve(Resolver resolver)
            {
                var writer = new StringWriter();

                writer.Write(Template.ItemGroups.ChooseBegin);

                foreach (KeyValuePair<string, List<IResolvable>> keyValuePair in Choices)
                {
                    using (resolver.NewScopedParameter("condition", keyValuePair.Key))
                    {
                        writer.Write(resolver.Resolve(Template.ItemGroups.ChooseConditionBegin));

                        var itemGroupWriter = new StringWriter();

                        itemGroupWriter.Write(Template.ItemGroups.ItemGroupBegin);
                        foreach (IResolvable item in keyValuePair.Value)
                        {
                            itemGroupWriter.Write(item.Resolve(resolver));
                        }
                        itemGroupWriter.Write(Template.ItemGroups.ItemGroupEnd);

                        // Fix indentation here...
                        List<string> itemGroupResult = itemGroupWriter.ToString().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => "    " + s).ToList();
                        itemGroupResult.ForEach(writer.WriteLine);

                        writer.Write(Template.ItemGroups.ChooseConditionEnd);
                    }
                }

                writer.Write(Template.ItemGroups.ChooseEnd);

                return resolver.Resolve(writer.ToString());
            }
        }

        public void Generate(Builder builder, Project project, List<Project.Configuration> configurations, string projectFile, List<string> generatedFiles, List<string> skipFiles)
        {
            _builder = builder;

            var fileInfo = new FileInfo(projectFile);
            string projectPath = fileInfo.Directory.FullName;
            string projectFileName = fileInfo.Name;

            if (!(project is CSharpProject))
                throw new ArgumentException("Project is not a CSharpProject");

            Generate((CSharpProject)project, configurations, projectPath, projectFileName, generatedFiles, skipFiles);
            _builder = null;
        }

        private Project.Configuration _projectConfiguration;
        private List<Project.Configuration> _projectConfigurationList;
        private string _projectPath;
        private string _projectPathCapitalized;
        private Builder _builder;
        public const string ProjectExtension = ".csproj";
        private const string RemoveLineTag = FileGeneratorUtilities.RemoveLineTag;


        private void SelectOption(params Options.OptionAction[] options)
        {
            Options.SelectOption(_projectConfiguration, options);
        }

        private static void Write(string value, TextWriter writer, Resolver resolver)
        {
            string resolvedValue = resolver.Resolve(value);
            var reader = new StringReader(resolvedValue);
            string str = reader.ReadToEnd();
            writer.Write(str);
            writer.Flush();
        }

        private string ReadGuidFromProjectFile(Project.Configuration dependency)
        {
            var guidFromProjectFile = Sln.ReadGuidFromProjectFile(dependency.ProjectFullFileNameWithExtension);
            return (string.IsNullOrEmpty(guidFromProjectFile)) ? RemoveLineTag : guidFromProjectFile;
        }

        private static TargetFramework GetTargetFramework(Project.Configuration conf)
        {
            var dotNetFramework = conf.Target.GetFragment<DotNetFramework>();
            DotNetOS dotNetOS;
            if (!conf.Target.TryGetFragment(out dotNetOS))
                dotNetOS = conf.DotNetOSVersion;
            return new TargetFramework(dotNetFramework, dotNetOS, conf.DotNetOSVersionSuffix);
        }

        private void Generate(
            CSharpProject project,
            List<Project.Configuration> unsortedConfigurations,
            string projectPath,
            string projectFile,
            List<string> generatedFiles,
            List<string> skipFiles
        )
        {
            var itemGroups = new ItemGroups();

            // Need to sort by name and platform
            List<Project.Configuration> configurations = unsortedConfigurations.OrderBy(conf => conf.Name + conf.Platform).ToList();

            var projectFrameworksPerConf = configurations.ToDictionary(conf => conf, GetTargetFramework);
            var projectFrameworks = projectFrameworksPerConf.Values.Distinct().ToList();
            itemGroups.SetTargetFrameworks(projectFrameworks);

            // valid that 2 conf name in the same project don't have the same name
            var configurationNameMapping = new Dictionary<string, Project.Configuration>();
            string assemblyName = null;
            foreach (Project.Configuration conf in configurations)
            {
                if (assemblyName == null)
                    assemblyName = conf.AssemblyName;
                else if (assemblyName != conf.AssemblyName)
                    throw new Error("The assemblyName can't be different between configurations of a same project. {0} != {1} in {2}", assemblyName, conf.AssemblyName, projectFile);

                //set the default outputType
                if (conf.Output == Project.Configuration.OutputType.Exe)
                    conf.Output = Project.Configuration.OutputType.DotNetConsoleApp;
                if (conf.Output == Project.Configuration.OutputType.Dll)
                    throw new Error("OutputType for C# projects must be either DotNetClassLibrary, DotNetConsoleApp or DotNetWindowsApp");

                string projectUniqueName = conf.Name + Util.GetToolchainPlatformString(conf.Platform, conf.Project, conf.Target);

                configurationNameMapping[projectUniqueName] = conf;

                // set generator information
                conf.GeneratorSetOutputFullExtensions(".exe", ".exe", ".dll", ".pdb");
            }

            string outputType;
            switch (configurations[0].Output)
            {
                case Project.Configuration.OutputType.DotNetWindowsApp:
                    outputType = "WinExe";
                    break;
                case Project.Configuration.OutputType.DotNetClassLibrary:
                    outputType = "Library";
                    break;
                case Project.Configuration.OutputType.DotNetConsoleApp:
                default:
                    outputType = "Exe";
                    break;
            }

            string projectTypeGuids = RemoveLineTag;
            switch (project.ProjectTypeGuids)
            {
                case CSharpProjectType.Test:
                    projectTypeGuids = ProjectTypeGuids.ToOption(ProjectTypeGuids.CSharpTestProject);
                    break;
                case CSharpProjectType.Vsix:
                    projectTypeGuids = ProjectTypeGuids.ToOption(ProjectTypeGuids.VsixProject);
                    break;
                case CSharpProjectType.Vsto:
                    projectTypeGuids = ProjectTypeGuids.ToOption(ProjectTypeGuids.VstoProject);
                    break;
                case CSharpProjectType.Wpf:
                    projectTypeGuids = ProjectTypeGuids.ToOption(ProjectTypeGuids.WpfProject);
                    break;
                case CSharpProjectType.Wcf:
                    projectTypeGuids = ProjectTypeGuids.ToOption(ProjectTypeGuids.WcfProject);
                    break;
                case CSharpProjectType.AspNetMvc5:
                    projectTypeGuids = ProjectTypeGuids.ToOption(ProjectTypeGuids.AspNetMvc5Project);
                    break;
            }

            var resolver = new Resolver();

            _projectPath = projectPath;
            _projectPathCapitalized = Util.GetCapitalizedPath(projectPath);
            _projectConfigurationList = configurations;

            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);

            string targetFrameworkString;
            DevEnv devenv = configurations.Select(
                    conf => conf.Target.GetFragment<DevEnv>()).Distinct().Single();

            bool isNetCoreProjectSchema = project.ProjectSchema == CSharpProjectSchema.NetCore ||
                                            (project.ProjectSchema == CSharpProjectSchema.Default &&
                                              (projectFrameworks.Any(x => x.DotNetFramework.IsDotNetCore() || x.DotNetFramework.IsDotNetStandard()) || projectFrameworks.Count > 1)
                                            );

            if (isNetCoreProjectSchema)
            {
                Write(Template.Project.ProjectBeginNetCore, writer, resolver);
                targetFrameworkString = GetTargetFrameworksString(projectFrameworks.ToArray());
            }
            else
            {
                var framework = projectFrameworks.Single().DotNetFramework;
                targetFrameworkString = Util.GetDotNetTargetString(framework);

                using (resolver.NewScopedParameter("toolsVersion", Util.GetToolVersionString(devenv)))
                {
                    // xml begin header
                    switch (devenv)
                    {
                        case DevEnv.vs2015:
                            Write(Template.Project.ProjectBegin, writer, resolver);
                            break;
                        case DevEnv.vs2017:
                        case DevEnv.vs2019:
                        case DevEnv.vs2022:
                            Write(Template.Project.ProjectBeginVs2017, writer, resolver);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            var preImportCustomProperties = new Dictionary<string, string>(project.PreImportCustomProperties);
            AddPreImportCustomProperties(preImportCustomProperties, project, projectPath);
            WriteCustomProperties(preImportCustomProperties, project, writer, resolver);

            var preImportProjects = new List<ImportProject>(project.PreImportProjects);
            WriteImportProjects(preImportProjects.Distinct(EqualityComparer<ImportProject>.Default), project, configurations.First(), writer, resolver);

            // generate all configuration options onces...
            var options = new Dictionary<Project.Configuration, Options.ExplicitOptions>();

            foreach (Project.Configuration conf in _projectConfigurationList)
            {
                _projectConfiguration = conf;
                Options.ExplicitOptions option = GenerateOptions(project, conf);
                _projectConfiguration = null;
                options.Add(conf, option);
            }

            string netCoreEnableDefaultItems = RemoveLineTag;
            string defaultItemExcludes = RemoveLineTag;
            string targetFrameworkVersionString = "TargetFrameworkVersion";
            string projectPropertyGuid = configurations[0].ProjectGuid;
            string projectConfigurationCondition = Template.Project.DefaultProjectConfigurationCondition;
            if (isNetCoreProjectSchema)
            {
                netCoreEnableDefaultItems = project.EnableDefaultItems.ToString().ToLowerInvariant();

                if (project.DefaultItemExcludes.Count > 0)
                {
                    defaultItemExcludes = string.Join(";", project.DefaultItemExcludes);
                }

                targetFrameworkVersionString = "TargetFramework";
                projectPropertyGuid = RemoveLineTag;
                if (projectFrameworks.Count() > 1)
                {
                    projectConfigurationCondition = Template.Project.MultiFrameworkProjectConfigurationCondition;
                    targetFrameworkVersionString = "TargetFrameworks";
                }
            }

            string restoreProjectStyleString = RemoveLineTag;
            if (project.ExplicitNugetRestoreProjectStyle != false && project.NuGetReferenceType != Project.NuGetPackageMode.VersionDefault)
            {
                switch (project.NuGetReferenceType)
                {
                    case Project.NuGetPackageMode.PackageReference:
                        restoreProjectStyleString = "PackageReference";
                        break;
                    case Project.NuGetPackageMode.PackageConfig:
                    case Project.NuGetPackageMode.ProjectJson:
                        throw new Error($"Unsupported explicit NuGetReferenceType \"{project.NuGetReferenceType}\"");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            GeneratedAssemblyConfigTemplate generatedAssemblyConfigTemplate = new GeneratedAssemblyConfigTemplate(project.GeneratedAssemblyConfig, isNetCoreProjectSchema, RemoveLineTag);

            using (resolver.NewScopedParameter("project", project))
            using (resolver.NewScopedParameter("guid", projectPropertyGuid))
            using (resolver.NewScopedParameter("options", options[_projectConfigurationList[0]]))
            using (resolver.NewScopedParameter("outputType", outputType))
            using (resolver.NewScopedParameter("targetFramework", targetFrameworkString))
            using (resolver.NewScopedParameter("targetFrameworkVersionString", targetFrameworkVersionString))
            using (resolver.NewScopedParameter("projectTypeGuids", projectTypeGuids))
            using (resolver.NewScopedParameter("assemblyName", assemblyName))
            using (resolver.NewScopedParameter("defaultPlatform", Util.GetToolchainPlatformString(project.DefaultPlatform ?? configurations[0].Platform, project, null)))
            using (resolver.NewScopedParameter("netCoreEnableDefaultItems", netCoreEnableDefaultItems))
            using (resolver.NewScopedParameter("defaultItemExcludes", defaultItemExcludes))
            using (resolver.NewScopedParameter("GeneratedAssemblyConfigTemplate", generatedAssemblyConfigTemplate))
            using (resolver.NewScopedParameter("NugetRestoreProjectStyleString", restoreProjectStyleString))
            using (resolver.NewScopedParameter("GenerateDocumentationFile", project.GenerateDocumentationFile ? "true" : RemoveLineTag))
            {
                Write(Template.Project.ProjectDescription, writer, resolver);
            }

            if (!string.IsNullOrEmpty(project.ApplicationIcon))
            {
                using (resolver.NewScopedParameter("iconpath", Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(project.ApplicationIcon))))
                    Write(Template.ApplicationIcon, writer, resolver);
            }

            string appManifest = project.ApplicationManifest;

            if (!string.IsNullOrEmpty(appManifest))
            {
                appManifest = project.NoneFiles.FirstOrDefault(s => s.EndsWith(appManifest, StringComparison.OrdinalIgnoreCase));
                if (appManifest != null)
                {
                    appManifest = Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(appManifest));
                    using (resolver.NewScopedParameter("applicationmanifest", appManifest))
                        Write(Template.ApplicationManifest, writer, resolver);
                }
            }

            if (!string.IsNullOrEmpty(project.StartupObject))
            {
                using (resolver.NewScopedParameter("startupobject", project.StartupObject))
                    Write(Template.StartupObject, writer, resolver);
            }

            if (project.NoWin32Manifest)
            {
                Write(Template.NoWin32Manifest, writer, resolver);
            }

            if (project.UseMSBuild14IfAvailable)
            {
                Write(Template.MSBuild14PropertyGroup, writer, resolver);
            }

            WriteCustomProperties(project.CustomProperties, project, writer, resolver);

            if (project.ProjectTypeGuids == CSharpProjectType.Wcf)
            {
                string wcfAutoStart = RemoveLineTag;
                if (project.WcfAutoStart.HasValue)
                    wcfAutoStart = project.WcfAutoStart.ToString();
                using (resolver.NewScopedParameter("AutoStart", wcfAutoStart))
                using (resolver.NewScopedParameter("WCFExtensionGUID", ProjectTypeGuids.WindowsCommunicationFoundation.ToString("B").ToUpper()))
                    Write(Template.ProjectExtensionsWcf, writer, resolver);
            }

            if (project.ProjectTypeGuids == CSharpProjectType.Vsto)
            {
                var vstoProject = project as CSharpVstoProject;

                if (vstoProject == null)
                    throw new InvalidDataException("VSTO project was not correctly initialized. Use CSharpVstoProject instead of CSharpProject");

                using (resolver.NewScopedParameter("AddInNamespace", project.RootNamespace))
                using (resolver.NewScopedParameter("OfficeApplication", vstoProject.Application.ToString()))
                using (resolver.NewScopedParameter("OfficeSDKVersion", vstoProject.OfficeSdkVersion))
                    Write(Template.ProjectExtensionsVsto, writer, resolver);
            }

            if (project.ProjectTypeGuids == CSharpProjectType.AspNetMvc5)
            {
                var aspnetProject = project as IAspNetProject;

                if (aspnetProject == null)
                    throw new InvalidDataException("AspNet project was not correctly initialized. Implement IAspNetProject interface");

                Func<bool?, string> toEnableDisable = (value) => value.HasValue ? (value.Value ? "enabled" : "disabled") : RemoveLineTag;

                using (resolver.NewScopedParameter("MvcBuildViews", aspnetProject.MvcBuildViews?.ToString() ?? RemoveLineTag))
                using (resolver.NewScopedParameter("UseIISExpress", aspnetProject.UseIISExpress?.ToString() ?? RemoveLineTag))
                using (resolver.NewScopedParameter("IISExpressSSLPort", aspnetProject.IISExpressSSLPort?.ToString() ?? RemoveLineTag))
                using (resolver.NewScopedParameter("IISExpressAnonymousAuthentication", toEnableDisable(aspnetProject.IISExpressAnonymousAuthentication)))
                using (resolver.NewScopedParameter("IISExpressWindowsAuthentication", toEnableDisable(aspnetProject.IISExpressWindowsAuthentication)))
                using (resolver.NewScopedParameter("IISExpressUseClassicPipelineMode", aspnetProject.IISExpressUseClassicPipelineMode?.ToString() ?? RemoveLineTag))
                using (resolver.NewScopedParameter("UseGlobalApplicationHostFile", aspnetProject.UseGlobalApplicationHostFile?.ToString() ?? RemoveLineTag))
                    Write(Template.Project.ProjectAspNetMvcDescription, writer, resolver);
            }

            var additionalNones = new List<string>();
            if (isNetCoreProjectSchema)
            {
                string launchSettingsJson = LaunchSettingsJson.Generate(_builder, project, projectPath, _projectConfigurationList, generatedFiles, skipFiles);
                if (launchSettingsJson != null)
                    additionalNones.Add(launchSettingsJson);
            }
            else
            {
                // old style cproj.user file
                string projectFilePath = Path.Combine(projectPath, projectFile) + ProjectExtension;
                UserFile uf = new UserFile(projectFilePath);
                uf.GenerateUserFile(_builder, project, _projectConfigurationList, generatedFiles, skipFiles);
            }

            // In case we need to swap out dependencies, we'll cache them here
            Dictionary<string, List<DotNetDependency>> swappedNamesToDependencies = null;

            // configuration general
            foreach (Project.Configuration conf in _projectConfigurationList)
            {
                using (resolver.NewScopedParameter("platformName", Util.GetToolchainPlatformString(conf.Platform, conf.Project, conf.Target)))
                using (resolver.NewScopedParameter("conf", conf))
                using (resolver.NewScopedParameter("project", project))
                using (resolver.NewScopedParameter("targetFramework", GetTargetFrameworksString(projectFrameworksPerConf[conf])))
                using (resolver.NewScopedParameter("projectConfigurationCondition", projectConfigurationCondition))
                using (resolver.NewScopedParameter("target", conf.Target))
                using (resolver.NewScopedParameter("options", options[conf]))
                {
                    Write(Template.PropertyGroupWithConditionStart, writer, resolver);
                    Write(Template.Project.ProjectConfigurationsGeneral, writer, resolver);
                    WriteProperties(conf.CustomProperties, writer, resolver);
                    Write(VsProjCommon.Template.PropertyGroupEnd, writer, resolver);
                }

                foreach (var dependencies in new[] { conf.DotNetPublicDependencies, conf.DotNetPrivateDependencies })
                {
                    foreach (var dependency in dependencies)
                    {
                        var dependencyConfiguration = dependency.Configuration;

                        if (!Util.IsDotNet(dependencyConfiguration))
                            continue;

                        if (dependency.ReferenceSwappedWithOutputAssembly)
                        {
                            // cache swapped dependencies and sort them out later since we have no visibility here regarding
                            // multiple frameworks or optimizations from within a single Project.Configuration
                            // Note: even if preallocating looks tempting, the time it takes to count the entries is actually longer than the time resizing
                            swappedNamesToDependencies ??= new Dictionary<string, List<DotNetDependency>>(StringComparer.Ordinal);
                            if (!swappedNamesToDependencies.TryAdd(dependency.Configuration.AssemblyName, new List<DotNetDependency>{ dependency }))
                            {
                                swappedNamesToDependencies[dependency.Configuration.AssemblyName].Add(dependency);
                            }
                        }
                        else
                        {
                            string dependencyExtension = Util.GetProjectFileExtension(dependencyConfiguration);
                            string projectFullFileNameWithExtension = Util.GetCapitalizedPath(dependencyConfiguration.ProjectFullFileName + dependencyExtension);
                            string relativeToProjectFile = Util.PathGetRelative(_projectPathCapitalized, projectFullFileNameWithExtension);

                            // If dependency project is marked as [Compile], read the GUID from the project file
                            if (dependencyConfiguration.Project.SharpmakeProjectType == Project.ProjectTypeAttribute.Compile && dependencyConfiguration.ProjectGuid == null)
                                dependencyConfiguration.ProjectGuid = ReadGuidFromProjectFile(dependencyConfiguration);

                            // FIXME : MsBuild does not seem to properly detect ReferenceOutputAssembly setting. 
                            // It may try to recompile the project if the output file of the dependency is missing. 
                            // To counter this, the CopyLocal field is forced to false for build-only dependencies. 
                            bool isPrivate = dependency.CopyLocal && project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.ProjectReferences) && dependency.ReferenceOutputAssembly != false;

                            string includeOutputGroupsInVsix = null;
                            if (isPrivate && project.ProjectTypeGuids == CSharpProjectType.Vsix)
                            {
                                // Includes debug symbols of private (i.e. copy local) referenced projects in the VSIX.
                                // This WILL override default values of <IncludeOutputGroupsInVSIX> and <IncludeOutputGroupsInVSIXLocalOnly> from Microsoft.VsSDK.targets,
                                // so if the VSIXs stop working, this may be the cause...
                                includeOutputGroupsInVsix = "DebugSymbolsProjectOutputGroup;BuiltProjectOutputGroup;BuiltProjectOutputGroupDependencies;GetCopyToOutputDirectoryItems;SatelliteDllsProjectOutputGroup";
                            }

                            itemGroups.ProjectReferences.Add(new ItemGroups.ProjectReference
                            {
                                Include = relativeToProjectFile,
                                Name = dependencyConfiguration.ProjectName,
                                Private = isPrivate,
                                Project = new Guid(dependencyConfiguration.ProjectGuid),
                                ReferenceOutputAssembly = dependency.ReferenceOutputAssembly,
                                IncludeOutputGroupsInVSIX = includeOutputGroupsInVsix,
                            });
                        }
                    }
                }

                foreach (var projectReferenceInfo in conf.ProjectReferencesByPath.ProjectsInfos)
                {
                    string projectFullFileNameWithExtension = Util.GetCapitalizedPath(projectReferenceInfo.projectFilePath);
                    string relativeToProjectFile = Util.PathGetRelative(_projectPathCapitalized,
                                                                        projectFullFileNameWithExtension);
                    var projectGuid = projectReferenceInfo.projectGuid;
                    if (projectGuid == Guid.Empty)
                        projectGuid = new Guid(Sln.ReadGuidFromProjectFile(projectReferenceInfo.projectFilePath));

                    itemGroups.ProjectReferences.Add(new ItemGroups.ProjectReference
                    {
                        Include = relativeToProjectFile,
                        Name = Path.GetFileNameWithoutExtension(projectReferenceInfo.projectFilePath),
                        Private =
                            project.DependenciesCopyLocal.HasFlag(
                                Project.DependenciesCopyLocalTypes
                                             .ProjectReferences),
                        Project = projectGuid,
                    });
                }
            }
            
            if(swappedNamesToDependencies is not null)
            {
                foreach (List<DotNetDependency> groupedDependencies in swappedNamesToDependencies.Values)
                {
                    bool isMultiFramework = groupedDependencies.Select(d => d.Configuration.Target.GetFragment<DotNetFramework>()).Distinct().Count() > 1;

                    foreach (var dependency in groupedDependencies.OrderByDescending(d => d.Configuration.Target.GetOptimization()))
                    {
                        TargetFramework targetFramework = GetTargetFramework(dependency.Configuration);
                        string dllPath = Path.Combine(dependency.Configuration.TargetPath, $"{dependency.Configuration.AssemblyName}{dependency.Configuration.DllFullExtension}");
                        var referencesByPath = new ItemGroups.Reference
                        {
                            Include = $"{dependency.Configuration.AssemblyName}{(isMultiFramework ? "-" + GetTargetFrameworksString(targetFramework) : "")}",
                            SpecificVersion = false,
                            HintPath = Util.PathGetRelative(_projectPathCapitalized, dllPath),
                            Private = dependency.CopyLocal && project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.ExternalReferences),
                        };
                        itemGroups.AddReference(targetFramework, referencesByPath);
                    }
                }
            }

            if (project.RunPostBuildEvent != Options.CSharp.RunPostBuildEvent.OnBuildSuccess)
            {
                using (resolver.NewScopedParameter("RunPostBuildEvent", Enum.GetName(typeof(Options.CSharp.RunPostBuildEvent), project.RunPostBuildEvent)))
                    Write(Template.Project.ProjectConfigurationsRunPostBuildEvent, writer, resolver);
            }

            string netCoreSdk = null;
            if (isNetCoreProjectSchema)
            {
                netCoreSdk = "Microsoft.NET.Sdk";
                if (project.NetCoreSdkType != NetCoreSdkTypes.Default)
                    netCoreSdk += "." + project.NetCoreSdkType.ToString();

                using (resolver.NewScopedParameter("importProject", "Sdk.props"))
                using (resolver.NewScopedParameter("sdkVersion", netCoreSdk))
                    Write(Template.Project.ImportProjectSdkItem, writer, resolver);
            }

            GenerateFiles(project, configurations, itemGroups, additionalNones, generatedFiles, skipFiles);

            #region <Choose> section
            Dictionary<string, List<IResolvable>> choiceDict =
                configurations.SelectMany(c => c.ConditionalReferencesByPath.Select(p => new Tuple<string, string>(c.ConditionalReferencesByPathCondition, p)))
                .GroupBy(t => t.Item1).ToDictionary(g => g.Key, g => g.Select(p => new ItemGroups.Reference
                {
                    Include = Path.GetFileNameWithoutExtension(p.Item2),
                    SpecificVersion = false,
                    HintPath = Util.PathGetRelative(_projectPathCapitalized, p.Item2),
                    Private = project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.ExternalReferences) ? default(bool?) : false
                } as IResolvable).ToList());

            if (choiceDict.Count != 0)
            {
                Choose choose = new Choose
                {
                    Choices = choiceDict
                };
                writer.Write(choose.Resolve(resolver));
            }
            #endregion

            #region WCF support
            // We need to know if there is any WCFMetadataStorage detected before generating <WCFMetadata>
            if (itemGroups.WCFMetadataStorages.Count > 0)
            {
                writer.Write(Template.ItemGroups.ItemGroupBegin);
                using (resolver.NewScopedParameter("baseStorage", project.WcfBaseStorage))
                    Write(Template.ItemGroups.WCFMetadata, writer, resolver);
                writer.Write(Template.ItemGroups.ItemGroupEnd);
            }
            #endregion

            using (resolver.NewScopedParameter("project", project))
                writer.Write(itemGroups.Resolve(resolver));

            var importProjects = new List<ImportProject>(project.ImportProjects);

            // For .NET Core the default import project is inferred instead of explicit.
            if (isNetCoreProjectSchema)
            {
                importProjects.RemoveAll(i => i.Project == CSharpProject.DefaultImportProject);
            }


            if (project.ProjectTypeGuids == CSharpProjectType.Vsix)
            {
                // Add an extra tag to setup VSIX on VS2017, which is generated on Visual Studio
                // 2017. (This is likely a Microsoft hack to plug 2017 on the 2015 toolset.)
                if (devenv.IsVisualStudio() && devenv >= DevEnv.vs2017)
                    writer.Write(CSproj.Template.Project.VsixConfiguration);
            }

            project.AddCSharpSpecificImportProjects(importProjects, devenv);

            // Add custom .targets files as import projects.
            foreach (string targetsFile in project.CustomTargetsFiles)
            {
                importProjects.AddRange(targetsFile.Select(f => new ImportProject() { Project = targetsFile }));
            }

            WriteImportProjects(importProjects.Distinct(EqualityComparer<ImportProject>.Default), project, configurations.First(), writer, resolver);

            foreach (var element in project.UsingTasks)
            {
                using (resolver.NewScopedParameter("project", project))
                using (resolver.NewScopedParameter("usingTaskElement", element))
                {
                    Write(Template.UsingTaskElement.UsingTask, writer, resolver);
                }
            }

            foreach (var element in project.CustomTargets)
            {
                using (resolver.NewScopedParameter("project", project))
                using (resolver.NewScopedParameter("targetElement", element))
                {
                    if (string.IsNullOrEmpty(element.TargetParameters))
                        Write(Template.TargetElement.CustomTargetNoParameters, writer, resolver);
                    else
                        Write(Template.TargetElement.CustomTarget, writer, resolver);
                }
            }

            if (project.ProjectTypeGuids == CSharpProjectType.AspNetMvc5)
            {
                var aspnetProject = project as IAspNetProject;

                if (aspnetProject == null)
                    throw new InvalidDataException("AspNet project was not correctly initialized. Use AspNetProject instead of CSharpProject");

                using (resolver.NewScopedParameter("UseIIS", aspnetProject.UseIIS?.ToString() ?? RemoveLineTag))
                using (resolver.NewScopedParameter("AutoAssignPort", aspnetProject.AutoAssignPort?.ToString() ?? RemoveLineTag))
                using (resolver.NewScopedParameter("DevelopmentServerPort", aspnetProject.DevelopmentServerPort?.ToString() ?? RemoveLineTag))
                using (resolver.NewScopedParameter("DevelopmentServerVPath", aspnetProject.DevelopmentServerVPath ?? RemoveLineTag))
                using (resolver.NewScopedParameter("IISUrl", aspnetProject.IISUrl ?? RemoveLineTag))
                using (resolver.NewScopedParameter("NTLMAuthentication", aspnetProject.NTLMAuthentication?.ToString() ?? RemoveLineTag))
                using (resolver.NewScopedParameter("UseCustomServer", aspnetProject.UseCustomServer?.ToString() ?? RemoveLineTag))
                using (resolver.NewScopedParameter("SaveServerSettingsInUserFile", aspnetProject.SaveServerSettingsInUserFile?.ToString() ?? RemoveLineTag))
                using (resolver.NewScopedParameter("AspNetMvc5ExtensionGUID", ProjectTypeGuids.AspNetMvc5.ToString("B").ToUpper()))
                    Write(Template.ProjectExtensionsAspNetMvc5, writer, resolver);
            }

            WriteEvents(options, writer, resolver);

            if (isNetCoreProjectSchema)
            {
                using (resolver.NewScopedParameter("importProject", "Sdk.targets"))
                using (resolver.NewScopedParameter("sdkVersion", netCoreSdk))
                    Write(Template.Project.ImportProjectSdkItem, writer, resolver);
            }

            Write(Template.Project.ProjectEnd, writer, resolver);

            // Write the project file
            writer.Flush();

            var cleanMemoryStream = Util.RemoveLineTags(memoryStream, RemoveLineTag);
            var projectFileInfo = new FileInfo(Path.Combine(projectPath, projectFile + ProjectExtension));
            if (_builder.Context.WriteGeneratedFile(project.GetType(), projectFileInfo, cleanMemoryStream))
                generatedFiles.Add(projectFileInfo.FullName);
            else
                skipFiles.Add(projectFileInfo.FullName);

            writer.Close();
        }

        private static string GetTargetFrameworksString(params TargetFramework[] projectFrameworks)
        {
            return string.Join(";", projectFrameworks.Select(tf =>
            {
                var dotNetFramework = tf.DotNetFramework;
                var dotNetOS = tf.DotNetOSVersion;
                var dotNetOSVersion = tf.DotNetOSVersionSuffix;

                if (dotNetOS == DotNetOS.Default || dotNetOS == 0)
                {
                    if (!string.IsNullOrEmpty(dotNetOSVersion))
                        throw new Error($"Can't set a {nameof(dotNetOSVersion)} ({dotNetOSVersion}) with {nameof(dotNetOS)} set to Default");
                    return dotNetFramework.ToFolderName();
                }

                return dotNetFramework.ToFolderName() + "-" + dotNetOS.ToString() + dotNetOSVersion;
            }));
        }

        public void AddPreImportCustomProperties(Dictionary<string, string> properties, CSharpProject cSharpProject, string projectPath)
        {
            if (!string.IsNullOrEmpty(cSharpProject.BaseIntermediateOutputPath))
            {
                var baseIntermediateOutputPath = Util.PathGetAbsolute(projectPath, cSharpProject.BaseIntermediateOutputPath);
                properties.Add(nameof(cSharpProject.BaseIntermediateOutputPath), baseIntermediateOutputPath);
            }
        }

        private static void WriteImportProjects(IEnumerable<ImportProject> importProjects, Project project, Project.Configuration conf, StreamWriter writer, Resolver resolver)
        {
            foreach (var import in importProjects)
            {
                using (resolver.NewScopedParameter("project", project))
                using (resolver.NewScopedParameter("importProject", import.Project))
                using (resolver.NewScopedParameter("conf", conf))
                {
                    if (!string.IsNullOrEmpty(import.Condition))
                        using (resolver.NewScopedParameter("importCondition", import.Condition))
                        {
                            Write(Template.Project.ImportProjectItem, writer, resolver);
                        }
                    else
                        Write(Template.Project.ImportProjectItemSimple, writer, resolver);
                }
            }
        }

        /// TODO: remove this and use <see cref="VsProjCommon.WriteCustomProperties"/> instead. Note <see cref="CSproj"/> should be migrated to  <see cref="IFileGenerator"/>
        private static void WriteCustomProperties(Dictionary<string, string> customProperties, Project project, StreamWriter writer, Resolver resolver)
        {
            if (customProperties.Any())
            {
                Write(VsProjCommon.Template.PropertyGroupStart, writer, resolver);
                WriteProperties(customProperties, writer, resolver);
                Write(VsProjCommon.Template.PropertyGroupEnd, writer, resolver);
            }
        }

        private static void WriteProperties(
            Dictionary<string, string> props,
            StreamWriter writer, 
            Resolver resolver
        )
        {
            foreach (KeyValuePair<string, string> kvp in props)
            {
                using (resolver.NewScopedParameter("custompropertyname", kvp.Key))
                using (resolver.NewScopedParameter("custompropertyvalue", kvp.Value))
                {
                    Write(VsProjCommon.Template.CustomProperty, writer, resolver);
                }
            }
        }

        internal enum CopyToOutputDirectory
        {
            Never,
            Always,
            PreserveNewest
        }

        private void GenerateFiles(
            CSharpProject project,
            List<Project.Configuration> configurations,
            ItemGroups itemGroups,
            IEnumerable<string> additionalNones,
            List<string> generatedFiles,
            List<string> skipFiles
        )
        {
            #region Content
            foreach (var file in project.ResolvedContentFullFileNames)
            {
                string include = Util.PathGetRelative(_projectPathCapitalized, file);
                itemGroups.Contents.Add(new ItemGroups.Content { Include = include, LinkFolder = project.GetLinkFolder(include) });
            }


            foreach (var content in project.AdditionalContentAlwaysCopy)
            {
                var includePath = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(content));
                itemGroups.Contents.Add(new ItemGroups.Content { Include = includePath, CopyToOutputDirectory = CopyToOutputDirectory.Always, LinkFolder = project.GetLinkFolder(includePath) });
            }

            foreach (var content in project.AdditionalContentCopyIfNewer)
            {
                string include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(content));
                itemGroups.Contents.Add(new ItemGroups.Content { Include = include, CopyToOutputDirectory = CopyToOutputDirectory.PreserveNewest, LinkFolder = project.GetLinkFolder(include) });
            }

            foreach (var content in project.AdditionalContent)
            {
                string include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(content));
                itemGroups.Contents.Add(new ItemGroups.Content { Include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(content)), LinkFolder = project.GetLinkFolder(include) });
            }

            foreach (var content in project.AdditionalContentAlwaysIncludeInVsix)
            {
                string include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(content));
                itemGroups.Contents.Add(new ItemGroups.Content { Include = include, IncludeInVsix = "true", LinkFolder = project.GetLinkFolder(include) });
            }

            foreach (var content in project.AdditionalNoneAlwaysCopy)
            {
                string include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(content));
                itemGroups.Nones.Add(new ItemGroups.None { Include = include, CopyToOutputDirectory = CopyToOutputDirectory.Always, LinkFolder = project.GetLinkFolder(include) });
            }

            foreach (var content in project.AdditionalNoneCopyIfNewer)
            {
                string include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(content));
                itemGroups.Nones.Add(new ItemGroups.None { Include = include, CopyToOutputDirectory = CopyToOutputDirectory.PreserveNewest, LinkFolder = project.GetLinkFolder(include) });
            }

            if (!string.IsNullOrWhiteSpace(project.ApplicationSplashScreen))
            {
                string include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(project.ApplicationSplashScreen));
                itemGroups.AppSplashScreen.Add(new ItemGroups.SplashScreen { Include = include });
            }

            foreach (var analyzerDllFilePath in project.AnalyzerDllFilePaths)
            {
                string include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(analyzerDllFilePath));
                itemGroups.Analyzers.Add(new ItemGroups.Analyzer { Include = include });
            }
            #endregion

            foreach (var glob in project.Globs)
            {
                itemGroups.Compiles.Add(new ItemGroups.Compile { Include = glob.Include, Exclude = glob.Exclude });
            }

            foreach (var embeddedResource in project.AdditionalEmbeddedResourceAlwaysCopy)
            {
                string file = Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(embeddedResource));
                itemGroups.EmbeddedResources.Add(new ItemGroups.EmbeddedResource { Include = file, CopyToOutputDirectory = CopyToOutputDirectory.Always, LinkFolder = project.GetLinkFolder(file) });
            }

            foreach (var embeddedResource in project.AdditionalEmbeddedResourceCopyIfNewer)
            {
                string file = Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(embeddedResource));
                itemGroups.EmbeddedResources.Add(new ItemGroups.EmbeddedResource { Include = file, CopyToOutputDirectory = CopyToOutputDirectory.PreserveNewest, LinkFolder = project.GetLinkFolder(file) });
            }

            if (configurations.SelectMany(config => config.AdditionalNone).Any(noneInclude => !configurations.First().AdditionalNone.Contains(noneInclude)))
            {
                throw new NotImplementedException("The None files are not the same between the configurations");
            }

            foreach (var vsctFile in project.VsctCompileFiles)
            {
                itemGroups.Vscts.Add(new ItemGroups.Vsct { Include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(vsctFile)) });
            }

            foreach (var vsdConfigXmlFile in project.VsdConfigXmlFiles)
            {
                itemGroups.VsdConfigXmls.Add(new ItemGroups.VsdConfigXml { Include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(vsdConfigXmlFile)) });
            }

            foreach (var vsixSourceItem in project.VSIXSourceItems)
            {
                itemGroups.VSIXSourceItems.Add(new ItemGroups.VSIXSourceItem { Include = vsixSourceItem });
            }

            foreach (var protoFile in project.ProtoFiles)
            {
                itemGroups.Protobufs.Add(new ItemGroups.Protobuf
                {
                    Include = Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(protoFile))
                });
            }

            HashSet<string> allContents = new HashSet<string>(itemGroups.Contents.Select(c => c.Include));
            List<string> resolvedSources = project.ResolvedSourceFiles.Select(source => Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(source))).ToList();
            List<string> resolvedResources = project.ResourceFiles.Concat(project.ResolvedResourcesFullFileNames).Select(resource => Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(resource))).Distinct().ToList();
            List<string> resolvedEmbeddedResource = project.ResourceFiles.Concat(project.AdditionalEmbeddedResource).Concat(project.AdditionalEmbeddedAssemblies).Select(f => Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(f))).Distinct().ToList();
            List<string> resolvedNoneFiles =
                (project.NoneFiles.Select(file => Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(file))))
                .Concat(project.AdditionalNone.Select(f => Util.PathGetRelative(_projectPathCapitalized, Path.GetFullPath(f))))
                .Where(f => !allContents.Contains(f) && !resolvedResources.Contains(f) && !resolvedEmbeddedResource.Contains(f)).Distinct().ToList();

            //Add the None files from the configuration
            resolvedNoneFiles = resolvedNoneFiles.Concat(
                configurations.First().AdditionalNone.Select(f => Util.PathGetRelative(_projectPathCapitalized, Path.GetFullPath(f)))).ToList();

            var resolvedNoneFilesAddIfNewer = project.NoneFilesCopyIfNewer
                .Select(file => Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(file)))
                .Where(f => !allContents.Contains(f) && !resolvedResources.Contains(f) && !resolvedEmbeddedResource.Contains(f)).Distinct().ToList();

            List<string> publicResources = project.PublicResourceFiles.Select(source => Util.PathGetRelative(_projectPathCapitalized, Project.GetCapitalizedFile(source))).ToList();

            #region exclusions
            // None file exclusions
            List<Regex> exclusionRegexs = project.SourceFilesExcludeRegex.Concat(project.SourceNoneFilesExcludeRegex).Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToList();
            resolvedNoneFiles = resolvedNoneFiles.Where(f =>
            {
                string filePath = Util.PathGetAbsolute(_projectPath, f); // LCTODO: taking the full path is too broad, and can vary depending on the location of the codebase! it needs to be fixed
                return exclusionRegexs.All(r => !r.IsMatch(filePath));
            }).ToList();
            resolvedNoneFilesAddIfNewer = resolvedNoneFilesAddIfNewer.Where(f =>
            {
                string filePath = Util.PathGetAbsolute(_projectPath, f);
                return exclusionRegexs.All(r => !r.IsMatch(filePath));
            }).ToList();
            #endregion

            var remainingSourcesFiles = new List<string>(resolvedSources);
            var remainingResourcesFiles = new List<string>(resolvedResources);
            var remainingEmbeddedResourcesFiles = new List<string>(resolvedEmbeddedResource);
            var remainingNoneFiles = new List<string>(resolvedNoneFiles);

            //add none files from the first part of the generation
            remainingNoneFiles.AddRange(
                additionalNones.Select(f => Util.PathGetRelative(_projectPathCapitalized, Path.GetFullPath(f))));

            #region global file association
            List<FileAssociation> fileAssociations = FullFileNameAssociation(remainingSourcesFiles.Concat(resolvedResources).Concat(resolvedEmbeddedResource).Concat(resolvedNoneFiles));

            foreach (FileAssociation fileAssociation in fileAssociations)
            {
                if (fileAssociation.Type == FileAssociationType.Unknown)
                    continue;

                switch (fileAssociation.Type)
                {
                    case FileAssociationType.Xaml:
                        {
                            string linkedCsFile = fileAssociation.GetFilenameWithExtension(".xaml.cs");
                            string xaml = fileAssociation.GetFilenameWithExtension(".xaml");
                            itemGroups.Compiles.Add(new ItemGroups.Compile
                            {
                                Include = linkedCsFile,
                                DependentUpon = Path.GetFileName(xaml),
                                LinkFolder = GetProjectLinkedFolder(linkedCsFile, _projectPathCapitalized, project)
                            });

                            itemGroups.Pages.Add(new ItemGroups.Page
                            {
                                Include = xaml,
                                IsApplicationDefinition = project.ApplicationDefinitionFilenames.Any(f => f.Equals(Path.GetFileName(xaml), StringComparison.OrdinalIgnoreCase)),
                                LinkFolder = GetProjectLinkedFolder(xaml, _projectPathCapitalized, project)
                            });
                            remainingSourcesFiles.Remove(xaml);
                            remainingSourcesFiles.Remove(linkedCsFile);
                            break;
                        }
                    case FileAssociationType.Designer:
                        {
                            string designerFile = fileAssociation.GetFilenameWithExtension(".designer.cs");
                            string csFile = fileAssociation.GetFilenameWithExtension(".cs");
                            itemGroups.Compiles.Add(new ItemGroups.Compile
                            {
                                Include = designerFile,
                                DependentUpon = Path.GetFileName(csFile),
                                LinkFolder = GetProjectLinkedFolder(designerFile, _projectPathCapitalized, project)
                            });
                            remainingSourcesFiles.Remove(designerFile);
                            string resXFile = fileAssociation.GetFilenameWithExtension(".resx");
                            if (resXFile != null)
                            {
                                itemGroups.EmbeddedResources.Add(new ItemGroups.EmbeddedResource
                                {
                                    Include = resXFile,
                                    DependUpon = Path.GetFileName(csFile),
                                    LinkFolder = GetProjectLinkedFolder(resXFile, _projectPathCapitalized, project)
                                });
                                remainingEmbeddedResourcesFiles.Remove(resXFile);
                                remainingResourcesFiles.Remove(resXFile);
                            }
                            break;
                        }
                    case FileAssociationType.VSTOMain:
                        {
                            string mainAddinCode = fileAssociation.GetFilenameWithExtension(".cs");
                            string designerCode = fileAssociation.GetFilenameWithExtension(".Designer.cs");
                            string designerXml = fileAssociation.GetFilenameWithExtension(".Designer.xml");
                            itemGroups.Compiles.Add(new ItemGroups.Compile
                            {
                                Include = mainAddinCode,
                                SubType = "Code",
                                LinkFolder =
                                    GetProjectLinkedFolder(mainAddinCode, _projectPathCapitalized, project)
                            });
                            itemGroups.Compiles.Add(new ItemGroups.Compile
                            {
                                Include = designerCode,
                                DependentUpon = designerXml,
                                LinkFolder =
                                    GetProjectLinkedFolder(mainAddinCode, _projectPathCapitalized, project)
                            });
                            itemGroups.Nones.Add(new ItemGroups.None
                            {
                                Include = designerXml,
                                DependentUpon = mainAddinCode,
                                LinkFolder =
                                    GetProjectLinkedFolder(mainAddinCode, _projectPathCapitalized, project)
                            });
                            remainingSourcesFiles.Remove(mainAddinCode);
                            remainingSourcesFiles.Remove(designerCode);
                            remainingNoneFiles.Remove(designerXml);
                            break;
                        }
                    case FileAssociationType.VSTORibbon:
                        {
                            string xmlFile = fileAssociation.GetFilenameWithExtension(".xml");
                            string csFile = fileAssociation.GetFilenameWithExtension(".cs");
                            itemGroups.Compiles.Add(new ItemGroups.Compile
                            {
                                Include = csFile,
                                LinkFolder = GetProjectLinkedFolder(csFile, _projectPathCapitalized, project)
                            });
                            itemGroups.EmbeddedResources.Add(new ItemGroups.EmbeddedResource
                            {
                                Include = xmlFile,
                                SubType = "Designer",
                                Generator = RemoveLineTag,
                                LinkFolder = GetProjectLinkedFolder(csFile, _projectPathCapitalized, project)
                            });
                            remainingEmbeddedResourcesFiles.Remove(xmlFile);
                            remainingNoneFiles.Remove(xmlFile);
                            break;
                        }
                    case FileAssociationType.ResX:
                        {
                            string csFile = fileAssociation.GetFilenameWithExtension(".cs");
                            string resXFile = fileAssociation.GetFilenameWithExtension(".resx");
                            itemGroups.EmbeddedResources.Add(new ItemGroups.EmbeddedResource
                            {
                                Include = resXFile,
                                DependUpon = Path.GetFileName(csFile),
                                LinkFolder = GetProjectLinkedFolder(resXFile, _projectPathCapitalized, project)
                            });
                            remainingEmbeddedResourcesFiles.Remove(resXFile);
                            remainingResourcesFiles.Remove(resXFile);
                            break;
                        }
                    case FileAssociationType.AutoGenResX:
                        {
                            string designerFile = fileAssociation.GetFilenameWithExtension(".designer.cs");
                            string resXFile = fileAssociation.GetFilenameWithExtension(".resx");
                            bool publicAccessModifiers = publicResources.Any(f => f.Equals(resXFile, StringComparison.OrdinalIgnoreCase));
                            itemGroups.Compiles.Add(new ItemGroups.Compile
                            {
                                Include = designerFile,
                                DependentUpon = Path.GetFileName(resXFile),
                                LinkFolder = GetProjectLinkedFolder(designerFile, _projectPathCapitalized, project),
                                AutoGen = true,
                                DesignTime = publicAccessModifiers ? (bool?)true : null
                            });
                            itemGroups.EmbeddedResources.Add(new ItemGroups.EmbeddedResource
                            {
                                Include = resXFile,
                                Generator = publicAccessModifiers ? "PublicResXFileCodeGenerator" : "ResXFileCodeGenerator",
                                MergeWithCto = resXFile.EndsWith("VSPackage.resx", StringComparison.OrdinalIgnoreCase) ? "true" : null,
                                LastGenOutput = Path.GetFileName(designerFile),
                                LinkFolder = GetProjectLinkedFolder(resXFile, _projectPathCapitalized, project),
                                SubType = "Designer"
                            });
                            remainingSourcesFiles.Remove(designerFile);
                            remainingEmbeddedResourcesFiles.Remove(resXFile);
                            remainingResourcesFiles.Remove(resXFile);
                            break;
                        }
                    case FileAssociationType.Settings:
                        {
                            string file = fileAssociation.GetFilenameWithExtension(".settings");
                            string generatedFile = fileAssociation.GetFilenameWithExtension(".designer.cs");
                            AddNoneGeneratedItem(itemGroups, file, generatedFile, "SettingsSingleFileGenerator", true, _projectPathCapitalized, project);
                            remainingSourcesFiles.Remove(generatedFile);
                            remainingNoneFiles.Remove(file);
                            break;
                        }
                    case FileAssociationType.WCF:
                        {
                            string file = fileAssociation.GetFilenameWithExtension(".svcmap");
                            string generatedFile = fileAssociation.GetFilenameWithExtension(".cs");
                            AddNoneGeneratedItem(itemGroups, file, generatedFile, "WCF Proxy Generator", false, _projectPathCapitalized, project);
                            remainingSourcesFiles.Remove(generatedFile);
                            remainingNoneFiles.Remove(file);

                            string wcfStorage = Path.GetDirectoryName(file);
                            if (wcfStorage.IndexOf(project.WcfBaseStorage, StringComparison.OrdinalIgnoreCase) < 0)
                                throw new Error($"WCF file storage \"{wcfStorage}\" does not match project.{nameof(project.WcfBaseStorage)}:\"{project.WcfBaseStorage}\"");

                            itemGroups.WCFMetadataStorages.Add(new ItemGroups.WCFMetadataStorage { Include = wcfStorage });
                            break;
                        }
                    case FileAssociationType.Edmx:
                        {
                            string edmxFile = fileAssociation.GetFilenameWithExtension(".edmx");
                            string generatedFile = fileAssociation.GetFilenameWithExtension(".designer.cs");
                            itemGroups.Compiles.Add(new ItemGroups.Compile
                            {
                                Include = generatedFile,
                                AutoGen = true,
                                DesignTime = true,
                                DependentUpon = Path.GetFileName(edmxFile)
                            });

                            itemGroups.EntityDeploys.Add(new ItemGroups.EntityDeploy
                            {
                                Include = edmxFile,
                                Generator = "EntityModelCodeGenerator",
                                LastGenOutput = Path.GetFileName(generatedFile)
                            });

                            remainingSourcesFiles.Remove(generatedFile);
                            remainingSourcesFiles.Remove(edmxFile);
                            remainingNoneFiles.Remove(edmxFile);

                            string diagramFile = fileAssociation.GetFilenameWithExtension(".edmx.diagram");
                            if (diagramFile != null)
                            {
                                itemGroups.Nones.Add(new ItemGroups.None { Include = diagramFile, DependentUpon = Path.GetFileName(edmxFile) });
                                remainingNoneFiles.Remove(diagramFile);
                            }

                            string ttFile = fileAssociation.GetFilenameWithExtension(TTExtension);
                            if (ttFile != null)
                            {
                                string csModelFile = fileAssociation.GetFilenameWithExtension(".cs");
                                Trace.Assert(csModelFile != null);

                                itemGroups.Nones.Add(new ItemGroups.None
                                {
                                    Include = ttFile,
                                    Generator = "TextTemplatingFileGenerator",
                                    DependentUpon = Path.GetFileName(edmxFile),
                                    LastGenOutput = Path.GetFileName(csModelFile)
                                });

                                itemGroups.Compiles.Add(new ItemGroups.Compile
                                {
                                    Include = csModelFile,
                                    AutoGen = true,
                                    DesignTime = true,
                                    DependentUpon = Path.GetFileName(ttFile)
                                });

                                // Extract all potential generated classes generated for model from .edmx definition
                                XDocument edmxDoc = XDocument.Load(Path.Combine(_projectPathCapitalized, edmxFile));
                                XNamespace edmxNS = "http://schemas.microsoft.com/ado/2009/11/edmx";
                                XNamespace edmNS = "http://schemas.microsoft.com/ado/2009/11/edm";
                                XElement cm = edmxDoc.Descendants(edmxNS + "ConceptualModels").FirstOrDefault();
                                List<string> modelCsFiles = new List<string>();
                                if (cm != null)
                                {
                                    string modelDir = Path.GetDirectoryName(ttFile);
                                    modelCsFiles = cm.Descendants(edmNS + "EntitySet").Select(x => x.Attribute("EntityType").Value)
                                        .Where(f => !string.IsNullOrEmpty(f))
                                        .Select(f => Path.Combine(modelDir, f.Split('.').Last() + ".cs"))
                                        .Where(f => File.Exists(Path.Combine(_projectPathCapitalized, edmxFile)))
                                        .ToList();
                                }

                                modelCsFiles.ForEach(f => itemGroups.Compiles.Add(new ItemGroups.Compile
                                {
                                    Include = f,
                                    DependentUpon = Path.GetFileName(ttFile)
                                }));

                                remainingSourcesFiles.Remove(csModelFile);
                                remainingSourcesFiles.RemoveAll(f => modelCsFiles.Contains(f));
                                remainingNoneFiles.Remove(ttFile);
                            }

                            string contextttFile = fileAssociation.GetFilenameWithExtension(".context.tt");
                            if (contextttFile != null)
                            {
                                string csModelFile = fileAssociation.GetFilenameWithExtension(".context.cs");
                                Trace.Assert(csModelFile != null);

                                itemGroups.Nones.Add(new ItemGroups.None
                                {
                                    Include = contextttFile,
                                    Generator = "TextTemplatingFileGenerator",
                                    DependentUpon = Path.GetFileName(edmxFile),
                                    LastGenOutput = Path.GetFileName(csModelFile)
                                });

                                itemGroups.Compiles.Add(new ItemGroups.Compile
                                {
                                    Include = csModelFile,
                                    AutoGen = true,
                                    DesignTime = true,
                                    DependentUpon = Path.GetFileName(contextttFile)
                                });

                                remainingNoneFiles.Remove(contextttFile);
                                remainingSourcesFiles.Remove(csModelFile);
                            }

                            break;
                        }
                    case FileAssociationType.Asax:
                        {
                            string linkedCsFile = fileAssociation.GetFilenameWithExtension(".asax.cs");
                            string asax = fileAssociation.GetFilenameWithExtension(".asax");
                            itemGroups.Compiles.Add(new ItemGroups.Compile
                            {
                                Include = linkedCsFile,
                                DependentUpon = Path.GetFileName(asax),
                                LinkFolder = GetProjectLinkedFolder(linkedCsFile, _projectPathCapitalized, project)
                            });

                            itemGroups.Contents.Add(new ItemGroups.Content
                            {
                                Include = asax,
                            });
                            remainingSourcesFiles.Remove(asax);
                            remainingSourcesFiles.Remove(linkedCsFile);
                            break;
                        }
                    case FileAssociationType.XSD:
                        {
                            string xsdFile = fileAssociation.GetFilenameWithExtension(".xsd");
                            string xsxFile = fileAssociation.GetFilenameWithExtension(".xsx");

                            if (xsxFile != null)
                            {
                                itemGroups.Nones.Add(new ItemGroups.None()
                                {
                                    Include = xsdFile
                                });
                                itemGroups.Nones.Add(new ItemGroups.None()
                                {
                                    Include = xsxFile,
                                    DependentUpon = xsdFile
                                });

                                remainingNoneFiles.Remove(xsxFile);
                            }
                            else
                            {
                                string designerFile = fileAssociation.GetFilenameWithExtension(".designer.cs");
                                string csFile = fileAssociation.GetFilenameWithExtension(".cs");
                                string xscFile = fileAssociation.GetFilenameWithExtension(".xsc");
                                string xssFile = fileAssociation.GetFilenameWithExtension(".xss");

                                itemGroups.Nones.Add(new ItemGroups.None()
                                {
                                    Include = xsdFile,
                                    Generator = "MSDataSetGenerator",
                                    LastGenOutput = designerFile
                                });
                                itemGroups.Nones.Add(new ItemGroups.None()
                                {
                                    Include = xscFile,
                                    DependentUpon = xsdFile
                                });
                                itemGroups.Nones.Add(new ItemGroups.None()
                                {
                                    Include = xssFile,
                                    DependentUpon = xsdFile
                                });

                                itemGroups.Compiles.Add(new ItemGroups.Compile
                                {
                                    Include = designerFile,
                                    DependentUpon = xsdFile,
                                    AutoGen = true,
                                    DesignTime = true
                                });
                                itemGroups.Compiles.Add(new ItemGroups.Compile
                                {
                                    Include = csFile,
                                    DependentUpon = xsdFile
                                });

                                remainingSourcesFiles.Remove(designerFile);
                                remainingSourcesFiles.Remove(csFile);
                                remainingNoneFiles.Remove(xscFile);
                                remainingNoneFiles.Remove(xssFile);
                            }

                            remainingNoneFiles.Remove(xsdFile);
                            break;
                        }
                    default:
                        {
                            throw new ArgumentException(string.Format("Unsupported fileassociation type {0}", fileAssociation.Type));
                        }
                }
            }
            #endregion

            CsProjSubTypesInfos csProjSubTypesInfos = DetermineWindowsFormsSubTypes(configurations, remainingSourcesFiles);

            if (csProjSubTypesInfos?.SubTypeInfos.Count > 0)
            {
                AllCsProjSubTypesInfos.Add(csProjSubTypesInfos);
            }

            #region remaining files

            //tt files
            List<string> ttFiles = remainingNoneFiles.Where(f => f.EndsWith(TTExtension, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (string ttFile in ttFiles)
            {
                bool runtimeTemplate = project.AdditionalRuntimeTemplates.Contains(ttFile);
                string expectedExtension =
                    runtimeTemplate ? ".cs" :
                    Util.GetTextTemplateDirectiveParam(Path.Combine(_projectPath, ttFile), "output", "extension") ?? ".cs";
                if (!expectedExtension.StartsWith(".", StringComparison.Ordinal))
                    expectedExtension = "." + expectedExtension;
                string fileNameWithoutExtension = ttFile.Substring(0, ttFile.Length - TTExtension.Length);
                string generatedFile = fileNameWithoutExtension + expectedExtension;
                string generator = runtimeTemplate
                                        ? "TextTemplatingFilePreprocessor"
                                        : "TextTemplatingFileGenerator";

                // Always include files generated by text templating (.tt), even if they don't exist yet.
                // These files are expected to be absent during a clean build, as they are generated
                // during the build process. Therefore, they cannot be left out of the csproj file.
                AddContentGeneratedItem(itemGroups, ttFile, generatedFile, generator, false, _projectPathCapitalized, project);

                remainingNoneFiles.Remove(ttFile);
                //Remove generated file wherever it is.
                remainingEmbeddedResourcesFiles.Remove(generatedFile);
                remainingResourcesFiles.Remove(generatedFile);
                remainingSourcesFiles.Remove(generatedFile);
                remainingNoneFiles.Remove(generatedFile);
                resolvedNoneFilesAddIfNewer.Remove(generatedFile);
            }

            //xaml files
            var xamlFiles = remainingSourcesFiles.Where(src => src.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)).ToList();//tolist to enable removal in the foreach
            foreach (var xaml in xamlFiles)
            {
                //single XamlFile
                itemGroups.Pages.Add(new ItemGroups.Page
                {
                    Include = xaml,
                    IsApplicationDefinition = project.ApplicationDefinitionFilenames.Any(f => f.Equals(xaml, StringComparison.OrdinalIgnoreCase)),
                    LinkFolder = GetProjectLinkedFolder(xaml, _projectPathCapitalized, project)
                });
                remainingSourcesFiles.Remove(xaml);
            }

            foreach (var file in remainingNoneFiles)
            {
                itemGroups.Nones.Add(new ItemGroups.None { Include = file, LinkFolder = project.GetLinkFolder(file) });

                // Removing from remainingSourceFiles because sometimes the file is in both list. Could happen for example if
                // NoneExtensions contains .sharpmake.cs and SourceFilesExtensions contains .cs
                // If we don't remove the file, it will be duplicated in the csproj in a <Compile> section and in a <None> section.
                remainingSourcesFiles.Remove(file);
            }

            foreach (var remainingSourcesFile in remainingSourcesFiles)
            {
                itemGroups.Compiles.Add(new ItemGroups.Compile
                {
                    Include = remainingSourcesFile,
                    SubType = csProjSubTypesInfos?.SubTypeInfos.Find(s => string.Equals(s.FileName, remainingSourcesFile))?.SubType,
                    LinkFolder = GetProjectLinkedFolder(remainingSourcesFile, _projectPathCapitalized, project)
                });
            }

            //resources
            foreach (var file in remainingResourcesFiles)
            {
                // Have also as Resource for WPF
                if (project.IncludeResxAsResources || !file.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                {
                    itemGroups.Resources.Add(new ItemGroups.Resource
                    {
                        Include = file,
                        LinkFolder = GetProjectLinkedFolder(file, _projectPathCapitalized, project)
                    });
                }
            }

            foreach (var file in remainingEmbeddedResourcesFiles)
            {
                itemGroups.EmbeddedResources.Add(new ItemGroups.EmbeddedResource
                {
                    Include = file,
                    MergeWithCto = file.Equals("VSPackage.resx", StringComparison.OrdinalIgnoreCase) ? "true" : null,
                    LinkFolder = project.GetLinkFolder(file)
                });
            }

            foreach (var file in resolvedNoneFilesAddIfNewer)
            {
                itemGroups.Nones.Add(new ItemGroups.None { Include = file, CopyToOutputDirectory = CopyToOutputDirectory.PreserveNewest, LinkFolder = project.GetLinkFolder(file) });
            }

            #endregion

            #region References

            foreach (var conf in configurations)
            {
                var dotNetFramework = conf.Target.GetFragment<DotNetFramework>();
                if (dotNetFramework.IsDotNetFramework())
                {
                    foreach (var str in conf.ReferencesByName)
                    {
                        var referencesByName = new ItemGroups.Reference
                        {
                            Include = str,
                            Private = project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.DotNetReferences) ? default(bool?) : false,
                        };
                        itemGroups.AddReference(GetTargetFramework(conf), referencesByName);
                    }
                }
            }

            foreach (var conf in configurations)
            {
                var dotNetFramework = conf.Target.GetFragment<DotNetFramework>();
                foreach (var str in conf.ReferencesByNameExternal)
                {
                    var referencesByNameExternal = new ItemGroups.Reference
                    {
                        Include = str,
                        Private = project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.DotNetExtensions),
                    };
                    itemGroups.AddReference(GetTargetFramework(conf), referencesByNameExternal);
                }
            }

            foreach (var conf in configurations)
            {
                var dotNetFramework = conf.Target.GetFragment<DotNetFramework>();
                foreach (var str in conf.ReferencesByPath.Select(Util.GetCapitalizedPath))
                {
                    var referencesByPath = new ItemGroups.Reference
                    {
                        Include = Path.GetFileNameWithoutExtension(str),
                        SpecificVersion = false,
                        HintPath = Util.PathGetRelative(_projectPathCapitalized, str),
                        Private = project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.ExternalReferences),
                    };
                    itemGroups.AddReference(GetTargetFramework(conf), referencesByPath);
                }

                foreach (var str in project.AdditionalEmbeddedAssemblies.Select(Util.GetCapitalizedPath))
                {
                    var referencesByPath = new ItemGroups.Reference
                    {
                        Include = Path.GetFileNameWithoutExtension(str),
                        SpecificVersion = false,
                        HintPath = Util.PathGetRelative(_projectPathCapitalized, str),
                        Private = false
                    };
                    itemGroups.AddReference(GetTargetFramework(conf), referencesByPath);
                }
            }

            foreach (var conf in configurations)
            {
                var dotNetFramework = conf.Target.GetFragment<DotNetFramework>();
                if (dotNetFramework.IsDotNetFramework())
                {
                    foreach (var r in conf.DotNetReferences)
                    {
                        var references = GetItemGroupsReference(r, project.DependenciesCopyLocal);
                        itemGroups.AddReference(GetTargetFramework(conf), references);
                    }
                }
            }

            if (Util.DirectoryExists(Path.Combine(project.SourceRootPath, "Web References")))
                itemGroups.WebReferences.Add(new ItemGroups.WebReference { Include = @"Web References\" });

            itemGroups.WebReferences.AddRange(
                project.WebReferences.Select(str => new ItemGroups.WebReference { Include = str }));

            itemGroups.FolderIncludes.AddRange(project.AdditionalFolders.Select(str => new ItemGroups.FolderInclude { Include = str }));

            foreach (var url in project.WebReferenceUrls)
            {
                itemGroups.WebReferenceUrls.Add(
                    new ItemGroups.WebReferenceUrl
                    {
                        Include = url.Name,
                        UrlBehavior = url.UrlBehavior,
                        RelPath = url.RelPath,
                        UpdateFromURL = url.UpdateFromURL,
                        ServiceLocationURL = url.ServiceLocationURL,
                        CachedDynamicPropName = url.CachedDynamicPropName,
                        CachedAppSettingsObjectName = url.CachedAppSettingsObjectName,
                        CachedSettingsPropName = url.CachedSettingsPropName
                    });
            }

            bool propagationFlag = project.DependenciesCopyLocal.HasFlag(Project.DependenciesCopyLocalTypes.ExternalReferences);
            foreach (var comRef in project.ComReferences)
            {
                bool? privateValue = comRef.Private;
                if (!privateValue.HasValue && propagationFlag)
                {
                    // Only use the project propagation flag is not explicit value was provided and if the project propagation flag is set.
                    privateValue = propagationFlag;
                }
                itemGroups.ComReferences.Add(
                    new ItemGroups.ComReference
                    {
                        Include = comRef.Name,
                        Guid = comRef.Guid,
                        VersionMajor = comRef.VersionMajor,
                        VersionMinor = comRef.VersionMinor,
                        Lcid = comRef.Lcid,
                        WrapperTool = comRef.WrapperTool.ToString(),
                        Private = privateValue,
                        EmbedInteropTypes = comRef.EmbedInteropTypes,
                    });
            }

            GeneratePackageReferences(project, configurations, itemGroups, generatedFiles, skipFiles);

            foreach (var configuration in configurations)
            {
                var tf = GetTargetFramework(configuration);
                foreach (var frameworkReference in configuration.FrameworkReferences)
                {
                    itemGroups.AddFrameworkReference(new ItemGroups.FrameworkReference { Include = frameworkReference },
                        tf);
                }
            }

            itemGroups.Services.AddRange(project.Services.Select(s => new ItemGroups.Service { Include = s }));

            itemGroups.BootstrapperPackages.AddRange(project.BootstrapperPackages.Select(
                b =>
                new ItemGroups.BootstrapperPackage()
                {
                    Include = b.Include,
                    Install = b.Install,
                    ProductName = b.ProductName,
                    Visible = b.Visible,
                }
                ));

            itemGroups.FileAssociationItems.AddRange(project.FileAssociationItems.Select(
                b =>
                    new ItemGroups.FileAssociationItem()
                    {
                        Include = b.Include,
                        Visible = b.Visible,
                        Description = b.Description,
                        Progid = b.Progid,
                        DefaultIcon = b.DefaultIcon
                    }
            ));

            itemGroups.PublishFiles.AddRange(project.PublishFiles.Select(
                b =>
                    new ItemGroups.PublishFile()
                    {
                        Include = b.Include,
                        Visible = b.Visible,
                        Group = b.Group,
                        PublishState = b.PublishState,
                        IncludeHash = b.IncludeHash,
                        FileType = b.FileType
                    }
            ));
            #endregion
        }

        private ItemGroups.Reference GetItemGroupsReference(DotNetReference reference, Project.DependenciesCopyLocalTypes projectDependenciesCopyLocal)
        {
            string hintPath = !string.IsNullOrEmpty(reference.HintPath)
                ? Util.PathGetRelative(_projectPathCapitalized, reference.HintPath)
                : null;

            Project.DependenciesCopyLocalTypes typeToCheck = Project.DependenciesCopyLocalTypes.None;
            switch (reference.Type)
            {
                case DotNetReference.ReferenceType.Project:
                    typeToCheck = Project.DependenciesCopyLocalTypes.ProjectReferences;
                    break;
                case DotNetReference.ReferenceType.DotNet:
                    typeToCheck = Project.DependenciesCopyLocalTypes.DotNetReferences;
                    break;
                case DotNetReference.ReferenceType.DotNetExtensions:
                    typeToCheck = Project.DependenciesCopyLocalTypes.DotNetExtensions;
                    break;
                case DotNetReference.ReferenceType.External:
                    typeToCheck = Project.DependenciesCopyLocalTypes.ExternalReferences;
                    break;
            }

            bool? isPrivate = projectDependenciesCopyLocal.HasFlag(typeToCheck);

            return new ItemGroups.Reference()
            {
                Include = reference.Include,
                HintPath = hintPath,
                LinkFolder = reference.LinkFolder,
                Private = isPrivate,
                SpecificVersion = reference.SpecificVersion,
                EmbedInteropTypes = reference.EmbedInteropTypes,
            };
        }

        private void GeneratePackageReferences(
            CSharpProject project,
            List<Project.Configuration> configurations,
            ItemGroups itemGroups,
            List<string> generatedFiles,
            List<string> skipFiles
        )
        {
            foreach (var configuration in configurations)
            {
                var devenv = configuration.Target.GetFragment<DevEnv>();
                var targetFramework = GetTargetFramework(configuration);
                // package reference: Default in vs2017+
                if (project.NuGetReferenceType == Project.NuGetPackageMode.PackageReference
                    || (project.NuGetReferenceType == Project.NuGetPackageMode.VersionDefault && devenv >= DevEnv.vs2017))
                {
                    if (devenv < DevEnv.vs2017)
                        throw new Error("Package references are not supported on Visual Studio versions below vs2017");

                    var resolver = new Resolver();
                    foreach (var packageReference in configuration.ReferencesByNuGetPackage)
                    {
                        itemGroups.AddPackageReference(targetFramework, new ItemGroups.ItemTemplate(packageReference.Resolve(resolver)));
                    }
                }
                // project.json: Default in vs2015
                else if (project.NuGetReferenceType == Project.NuGetPackageMode.ProjectJson
                        || (project.NuGetReferenceType == Project.NuGetPackageMode.VersionDefault && devenv == DevEnv.vs2015))
                {
                    if (devenv < DevEnv.vs2015)
                        throw new Error("Project.json files are not supported on Visual Studio versions below vs2015");

                    var projectJson = new ProjectJson();
                    projectJson.Generate(_builder, project, configurations, _projectPath, generatedFiles, skipFiles);
                    if (projectJson.IsGenerated)
                    {
                        string include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(projectJson.ProjectJsonPath));
                        itemGroups.Nones.Add(new ItemGroups.None { Include = include });
                    }
                }
                // packages.config: only if manually chosen
                else if (project.NuGetReferenceType == Project.NuGetPackageMode.PackageConfig)
                {
                    var packagesConfig = new PackagesConfig();
                    packagesConfig.Generate(_builder, project, configurations, _projectPath, generatedFiles, skipFiles);
                    if (packagesConfig.IsGenerated)
                    {
                        string include = Util.PathGetRelative(_projectPathCapitalized, Util.SimplifyPath(packagesConfig.PackagesConfigPath));
                        itemGroups.Nones.Add(new ItemGroups.None { Include = include });
                    }
                    foreach (var references in configuration.ReferencesByNuGetPackage)
                    {
                        string dotNetHint = references.DotNetHint;
                        if (string.IsNullOrWhiteSpace(dotNetHint))
                        {
                            var frameworkFlags = project.Targets.TargetPossibilities.Select(f => f.GetFragment<DotNetFramework>()).Aggregate((x, y) => x | y);
                            DotNetFramework dnfs = ((DotNetFramework[])Enum.GetValues(typeof(DotNetFramework))).First(f => frameworkFlags.HasFlag(f));
                            dotNetHint = dnfs.ToFolderName();
                        }
                        string hintPath = Path.Combine("$(SolutionDir)packages", references.Name + "." + references.Version, "lib", dotNetHint, references.Name + ".dll");
                        itemGroups.AddReference(targetFramework, new ItemGroups.Reference { Include = references.Name, HintPath = hintPath });
                    }
                }
            }
        }

        private static void AddNoneGeneratedItem(ItemGroups itemGroups, string file, string generatedFile, string generator, bool designTimeSharedInput, string projectPath, Project project)
        {
            Trace.Assert(!string.IsNullOrEmpty(file) && !string.IsNullOrEmpty(generatedFile));
            itemGroups.Nones.Add(new ItemGroups.None
            {
                Include = file,
                Generator = generator,
                LastGenOutput = Path.GetFileName(generatedFile),
                LinkFolder = GetProjectLinkedFolder(file, projectPath, project)
            });
            var compile = new ItemGroups.Compile
            {
                Include = generatedFile,
                AutoGen = true,
                DependentUpon = Path.GetFileName(file),
                LinkFolder = GetProjectLinkedFolder(generatedFile, projectPath, project)
            };
            if (designTimeSharedInput)
                compile.DesignTimeSharedInput = true;
            else
                compile.DesignTime = true;
            itemGroups.Compiles.Add(compile);
        }


        /// <summary>
        /// Add the template file and the generated file to the project
        /// when requested by addGeneratedFile.
        /// .cs, .xaml and other are threated as different items.
        /// </summary>
        private static void AddContentGeneratedItem(
            ItemGroups itemGroups,
            string templateFile,
            string generatedFile,
            string generator,
            bool designTimeSharedInput,
            string projectPath,
            CSharpProject project)
        {
            Trace.Assert(!string.IsNullOrEmpty(templateFile) && !string.IsNullOrEmpty(generatedFile));
            itemGroups.Contents.Add(new ItemGroups.Content
            {
                Include = templateFile,
                Generator = generator,
                LastGenOutput = Path.GetFileName(generatedFile),
                LinkFolder = GetProjectLinkedFolder(templateFile, projectPath, project)
            });

            var generatedFileExtension = Path.GetExtension(generatedFile).ToLower();

            //TODO Give some kind of additional TT directive to specify the build action directly?
            //For now everything is none but cs and xaml.
            switch (generatedFileExtension)
            {
                case ".cs":
                    {
                        var compile = new ItemGroups.Compile
                        {
                            Include = generatedFile,
                            AutoGen = true,
                            DependentUpon = Path.GetFileName(templateFile),
                            LinkFolder = GetProjectLinkedFolder(generatedFile, projectPath, project)
                        };
                        if (designTimeSharedInput)
                            compile.DesignTimeSharedInput = true;
                        else
                            compile.DesignTime = true;
                        itemGroups.Compiles.Add(compile);
                        break;
                    }
                case ".xaml":
                    {
                        itemGroups.Pages.Add(new ItemGroups.Page
                        {
                            Include = generatedFile,
                            AutoGen = true,
                            DependentUpon = Path.GetFileName(templateFile),
                            LinkFolder = GetProjectLinkedFolder(generatedFile, projectPath, project),
                        });
                        break;
                    }
                default:
                    {
                        itemGroups.Nones.Add(new ItemGroups.None
                        {
                            Include = generatedFile,
                            LinkFolder = project.GetLinkFolder(generatedFile),
                            DependentUpon = Path.GetFileName(templateFile),
                        });
                        break;
                    }
            }
        }

        [DebuggerDisplay("[{Type}]{BaseFilePath}")]
        private class FileAssociation
        {
            public string BaseFilePath;
            public FileAssociationType Type;
            public List<string> Extensions;

            public string GetFilenameWithExtension(string ext)
            {
                string foundExt = Extensions.FirstOrDefault(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
                if (foundExt == null)
                    return null;
                return BaseFilePath + foundExt;
            }
        }

        private enum FileAssociationType
        {
            Unknown,
            Xaml,
            ResX,
            AutoGenResX,
            Designer,
            Settings,
            WCF,
            Edmx,
            VSTORibbon,
            VSTOMain,
            Asax,
            XSD
        };

        private static readonly List<Tuple<FileAssociationType, string[]>> s_fileExtensionsToType = new List<Tuple<FileAssociationType, string[]>>()
        {
            Tuple.Create(FileAssociationType.XSD, new []{".xsd", ".cs", ".designer.cs", ".xsc", ".xss"}),
            Tuple.Create(FileAssociationType.XSD, new []{".xsd", ".xsx" }),
            Tuple.Create(FileAssociationType.Designer, new []{".cs", ".resx", ".designer.cs"}),
            Tuple.Create(FileAssociationType.VSTOMain, new []{".cs", ".designer.xml", ".designer.cs" }),
            Tuple.Create(FileAssociationType.ResX, new []{".resx", ".cs"}),
            Tuple.Create(FileAssociationType.AutoGenResX, new []{".resx", ".designer.cs"}),
            Tuple.Create(FileAssociationType.Xaml, new []{".xaml", ".xaml.cs"}),
            Tuple.Create(FileAssociationType.Settings, new []{".settings", ".designer.cs"}),
            Tuple.Create(FileAssociationType.WCF, new []{".svcmap", ".cs"}),
            Tuple.Create(FileAssociationType.Edmx, new []{".edmx", ".designer.cs"}),
            Tuple.Create(FileAssociationType.Designer, new []{".cs", ".designer.cs"}),
            Tuple.Create(FileAssociationType.VSTORibbon, new []{".cs", ".xml"}),
            Tuple.Create(FileAssociationType.Asax, new []{".asax", ".asax.cs"}),
        };

        private static readonly string[] s_additionalFileExtensions = { ".tt", ".context.tt", ".context.cs", ".edmx.diagram" };

        private static List<FileAssociation> FullFileNameAssociation(IEnumerable<string> relatedFullFileNames)
        {
            List<string> supportedExts =
                s_fileExtensionsToType.SelectMany(t => t.Item2)
                .Concat(s_additionalFileExtensions)
                .Distinct().OrderByDescending(s => s.Length).ToList();

            var group = relatedFullFileNames
                .Where(s => supportedExts.Any(x => s.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(s =>
                {
                    string dirName = Path.GetDirectoryName(s);
                    string filename = Path.GetFileName(s);
                    string ext = supportedExts.First(e => filename.EndsWith(e, StringComparison.OrdinalIgnoreCase));
                    filename = filename.Substring(0, filename.LastIndexOf(ext, StringComparison.OrdinalIgnoreCase));
                    return Path.Combine(dirName, filename);
                });

            return group.Where(g => g.Count() > 1)
                .Select(g =>
                {
                    IEnumerable<string> exts = g.Select(s => s.Replace(g.Key, string.Empty));
                    return new FileAssociation()
                    {
                        BaseFilePath = g.Key,
                        Type = GetFileAssociationType(exts),
                        Extensions = exts.ToList()
                    };
                })
                .ToList();
        }

        private static FileAssociationType GetFileAssociationType(IEnumerable<string> fileExtensions)
        {
            var exts = fileExtensions.Select(s => s.ToLower()).ToList();

            Tuple<FileAssociationType, string[]> detectedType = s_fileExtensionsToType.FirstOrDefault(t => t.Item2.All(e => exts.Contains(e)));
            if (detectedType != null)
            {
                return detectedType.Item1;
            }
            return FileAssociationType.Unknown;
        }

        /// <summary>
        /// Gets a string meant to be used as a ItemGroupItem.LinkedFolder. This property controlls how the items get organised
        /// in the Solution Explorer in Visual Studio, otherwise known as filters. 
        /// 
        /// For relative paths, a filter is created by removing any "traverse parent folder" (../) elements from the beginning 
        /// of the path and using the remaining folder structure. 
        /// 
        /// For absolute paths, the drive letter is removed and the remaining folder structuer is used. 
        /// </summary>
        /// <param name="sourceFile">Path to the ItemGroupItem's file.</param>
        /// <param name="projectPath">Path to the folder in which the project file will be located.</param>
        /// <param name="project">The Project which the ItemGroupItem is a part of.</param>
        /// <returns>Returns null if the file is in or under the projectPath, meaning it's within the project's influencec and is not a link.
        /// Return empty string if the file is in the project.SourceRootPath or project.RootPath, not under it
        /// Returns a valid filter resembling a folder structure in any other case. 
        /// </returns>
        internal static string GetProjectLinkedFolder(string sourceFile, string projectPath, Project project)
        {
            // file is under the influence of the project and has no LinkFolder
            if (Util.PathIsUnderRoot(projectPath, sourceFile))
                return null;
            
            string absoluteFile = Util.PathGetAbsolute(projectPath, sourceFile);
            var directoryName = Path.GetDirectoryName(absoluteFile);

            // for files under SourceRootPath or RootPath, we use the subfolder structure 
            if (Util.PathIsUnderRoot(project.SourceRootPath, directoryName))
                return directoryName.Substring(project.SourceRootPath.Length).Trim(Util._pathSeparators);

            if (Util.PathIsUnderRoot(project.RootPath, directoryName))
                return directoryName.Substring(project.RootPath.Length).Trim(Util._pathSeparators);
            
            // Files outside all three project folders with and aboslute path use the
            // entire folder structure without the drive letter as filter
            if (Path.IsPathFullyQualified(sourceFile))
            {
                var root = Path.GetPathRoot(directoryName);
                return directoryName.Substring(root.Length).Trim(Util._pathSeparators);
            }

            // Files outside all three project folders with relative paths use their
            // relative path with all the leading "traverse parent folder" (../) removed
            // Example: "../../project/source/" becomes "project/source/"
            var trimmedPath = Util.TrimAllLeadingDotDot(sourceFile);
            var fileName = Path.GetFileName(absoluteFile);

            return trimmedPath.Substring(0, trimmedPath.Length - fileName.Length).Trim(Util._pathSeparators);
        }

        private void WriteEvents(Dictionary<Project.Configuration, Options.ExplicitOptions> options, StreamWriter writer, Resolver resolver)
        {
            var firstConf = _projectConfigurationList.First();

            if ((firstConf.Project is CSharpProject) && (firstConf.Project as CSharpProject).ConfigurationSpecificEvents)
            {
                foreach (var conf in _projectConfigurationList)
                {
                    WriteEvents(conf, options[conf], true, writer, resolver);
                }
            }
            else
            {
                WriteEvents(firstConf, options[firstConf], false, writer, resolver);
            }
        }

        private void WriteEvents(Project.Configuration conf, Options.ExplicitOptions options, bool conditional, StreamWriter writer, Resolver resolver)
        {
            using (resolver.NewScopedParameter("platformName", Util.GetToolchainPlatformString(conf.Platform, conf.Project, conf.Target)))
            using (resolver.NewScopedParameter("conf", conf))
            using (resolver.NewScopedParameter("options", options))
            {
                if (conf.EventPreBuild.Count != 0)
                    Write(conditional ? Template.Project.ProjectConfigurationsPreBuildEventConditional : Template.Project.ProjectConfigurationsPreBuildEvent, writer, resolver);

                if (conf.EventPostBuild.Count != 0)
                    Write(conditional ? Template.Project.ProjectConfigurationsPostBuildEventConditional : Template.Project.ProjectConfigurationsPostBuildEvent, writer, resolver);
            }
        }

        [Serializable]
        public class CsProjSubTypesInfos
        {
            public string CsProjFullPath { get; set; }
            public DateTime LastWriteTime { get; set; }
            public List<SubTypeInfo> SubTypeInfos { get; set; }

            [Serializable]
            public class SubTypeInfo
            {
                public string FileName { get; set; }
                public DateTime LastWriteTime { get; set; }
                public string SubType { get; set; }
            }
        }

        private static readonly Regex s_winFormSubTypeRegex = new Regex("// SHARPMAKE GENERATED CSPROJ SUBTYPE : <SubType>([A-Za-z]*)</SubType>");
        private static object s_allCachedCsProjSubTypesInfosLock = new object();
        private static List<CsProjSubTypesInfos> s_allCachedCsProjSubTypesInfos;
        public static ConcurrentBag<CsProjSubTypesInfos> AllCsProjSubTypesInfos { get; } = new ConcurrentBag<CsProjSubTypesInfos>();

        private CsProjSubTypesInfos DetermineWindowsFormsSubTypes(List<Project.Configuration> configurations, List<string> sourceFiles)
        {
            lock (s_allCachedCsProjSubTypesInfosLock)
            {
                if (s_allCachedCsProjSubTypesInfos == null)
                {
                    var listTypes = Util.DeserializeAllCsprojSubTypesJson<List<CsProjSubTypesInfos>>();
                    s_allCachedCsProjSubTypesInfos = listTypes?.Where(p => p != null).ToList() ?? new List<CsProjSubTypesInfos>();
                }
            }

            List<string> unresolvedSourceFiles = new List<string>();
            Project.Configuration config = configurations.First();
            string csProjFullPath = config.ProjectFullFileNameWithExtension;
            string projectPath = config.ProjectPath;

            if (!File.Exists(csProjFullPath))
            {
                return null;
            }

            CsProjSubTypesInfos cachedCsprojSubTypesInfos = s_allCachedCsProjSubTypesInfos.Find(p => p.CsProjFullPath == csProjFullPath);
            DateTime csProjLastWriteTime = File.GetLastWriteTime(csProjFullPath);

            if (cachedCsprojSubTypesInfos != null && cachedCsprojSubTypesInfos.LastWriteTime.Equals(csProjLastWriteTime))
            {
                return cachedCsprojSubTypesInfos;
            }

            CsProjSubTypesInfos csProjSubTypesInfos = new CsProjSubTypesInfos
            {
                CsProjFullPath = csProjFullPath,
                LastWriteTime = csProjLastWriteTime,
                SubTypeInfos = new List<CsProjSubTypesInfos.SubTypeInfo>()
            };

            foreach (string sourceFile in sourceFiles)
            {
                // Skip .designer.cs files as we know they are not Windows Form files.
                if (sourceFile.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                string sourceFilePath = Path.Combine(projectPath, sourceFile);

                // Skip missing files
                if (!File.Exists(sourceFilePath))
                    continue;

                DateTime sourceFileLastWriteTime = File.GetLastWriteTime(sourceFilePath);
                CsProjSubTypesInfos.SubTypeInfo matchingCachedSubTypeInfo = cachedCsprojSubTypesInfos ==
                    null ? new CsProjSubTypesInfos.SubTypeInfo() : cachedCsprojSubTypesInfos.SubTypeInfos.Find(s => string.Equals(s.FileName, sourceFile, StringComparison.OrdinalIgnoreCase));

                if (matchingCachedSubTypeInfo != null && matchingCachedSubTypeInfo.LastWriteTime.Equals(sourceFileLastWriteTime))
                {
                    csProjSubTypesInfos.SubTypeInfos.Add(matchingCachedSubTypeInfo);
                    continue;
                }

                string sourceFileData = File.ReadAllText(sourceFilePath);
                Match match = s_winFormSubTypeRegex.Match(sourceFileData);

                if (match.Success)
                {
                    string subType = match.Groups[1].Value;
                    CsProjSubTypesInfos.SubTypeInfo subTypesInfo = new CsProjSubTypesInfos.SubTypeInfo
                    {
                        FileName = sourceFile,
                        LastWriteTime = sourceFileLastWriteTime,
                        SubType = subType
                    };
                    csProjSubTypesInfos.SubTypeInfos.Add(subTypesInfo);
                }
                else
                {
                    unresolvedSourceFiles.Add(sourceFile);
                }
            }

            if (unresolvedSourceFiles.Count > 0)
            {
                Dictionary<string, string> csprojSubTypes = ExtractSubTypesFromCsProjFile(csProjFullPath);

                foreach (KeyValuePair<string, string> subType in csprojSubTypes)
                {
                    string matchingSourceFile = unresolvedSourceFiles.Find(s => s == subType.Key);

                    if (string.IsNullOrEmpty(matchingSourceFile))
                        continue;

                    CsProjSubTypesInfos.SubTypeInfo subTypesInfo = new CsProjSubTypesInfos.SubTypeInfo
                    {
                        FileName = matchingSourceFile,
                        LastWriteTime = File.GetLastWriteTime(Path.Combine(projectPath, matchingSourceFile)),
                        SubType = subType.Value
                    };
                    csProjSubTypesInfos.SubTypeInfos.Add(subTypesInfo);
                }
            }

            return csProjSubTypesInfos;
        }

        private static Dictionary<string, string> ExtractSubTypesFromCsProjFile(string csProjFile)
        {
            Dictionary<string, string> subTypes = new Dictionary<string, string>();

            if (!File.Exists(csProjFile))
                return subTypes;

            XDocument csProjXml;
            try
            {
                csProjXml = XDocument.Load(csProjFile);
            }
            catch (Exception)
            {
                // Malformed Xml (could happen because of a multithreading issue)
                return subTypes;
            }

            XElement projectElement = csProjXml.Root;
            Trace.Assert(projectElement != null);

            List<XElement> itemGroupElements = projectElement.Elements()
                .Where(e => e.Name.LocalName == "ItemGroup")
                .Where(l => l.Elements().All(e => e.Name.LocalName == "Compile")).ToList();

            foreach (XElement itemGroupElement in itemGroupElements)
            {
                List<XElement> compileElements = itemGroupElement.Elements().ToList();
                foreach (XElement compileElement in compileElements)
                {
                    XAttribute includeAttribute = compileElement.Attribute("Include");
                    XElement subTypeElement = compileElement.Elements().ToList().Find(e => e.Name.LocalName == "SubType");

                    if (includeAttribute == null || subTypeElement == null)
                        continue;

                    subTypes.Add(includeAttribute.Value, subTypeElement.Value);
                }
            }

            return subTypes;
        }

        public class ProjectFile
        {
            public string FileName;
            public string FileNameSourceRelative;
            public string DirectorySourceRelative;
            public string FileNameProjectRelative;

            public ProjectFile(string fileName, string projectPathCapitalized, string projectSourceCapitalized)
            {
                FileName = Project.GetCapitalizedFile(fileName);

                FileNameProjectRelative = Util.PathGetRelative(projectPathCapitalized, FileName);
                FileNameSourceRelative = Util.PathGetRelative(projectSourceCapitalized, FileName);

                int lastPathSeparator = FileNameSourceRelative.LastIndexOf(Util.WindowsSeparator);
                if (lastPathSeparator != -1)
                {
                    DirectorySourceRelative = FileNameSourceRelative.Substring(0, lastPathSeparator);
                    DirectorySourceRelative = DirectorySourceRelative.Trim('.', Util.WindowsSeparator);
                }
                else
                    DirectorySourceRelative = "";
            }
        }

        #region DependencyCopy
        private void ProcessDependencyCopy(CSharpProject project, Project.Configuration conf)
        {
            if (conf.Output != Project.Configuration.OutputType.DotNetWindowsApp && !conf.ExecuteTargetCopy)
                return;

            if (conf.CopyDependenciesBuildStep != null)
                throw new NotImplementedException("CopyDependenciesBuildStep are not implemented with csproj.");

            var copies = ProjectOptionsGenerator.ConvertPostBuildCopiesToRelative(conf, conf.TargetPath);
            foreach (var copy in copies)
            {
                var sourceFile = copy.Key;
                var destinationFolder = copy.Value;

                conf.EventPostBuild.Add(conf.CreateTargetCopyCommand(sourceFile, destinationFolder, conf.TargetPath));
            }

            var envVarResolver = PlatformRegistry.Get<IPlatformDescriptor>(Platform.win64).GetPlatformEnvironmentResolver(
                new VariableAssignment("project", project),
                new VariableAssignment("target", conf),
                new VariableAssignment("conf", conf));

            foreach (var customEvent in conf.ResolvedEventPreBuildExe)
            {
                if (customEvent is Project.Configuration.BuildStepExecutable)
                {
                    var execEvent = (Project.Configuration.BuildStepExecutable)customEvent;

                    string relativeExecutableFile = Util.PathGetRelative(conf.TargetPath, execEvent.ExecutableFile);
                    conf.EventPreBuild.Add(
                        string.Format(
                            "{0} {1}",
                            Util.SimplifyPath(envVarResolver.Resolve(relativeExecutableFile)),
                            envVarResolver.Resolve(execEvent.ExecutableOtherArguments)
                        )
                    );
                }
                else if (customEvent is Project.Configuration.BuildStepCopy)
                {
                    var copyEvent = (Project.Configuration.BuildStepCopy)customEvent;
                    conf.EventPreBuild.Add(copyEvent.GetCopyCommand(conf.TargetPath, envVarResolver));
                }
                else
                {
                    throw new Error("Invalid type in PreBuild steps");
                }
            }

            foreach (var customEvent in conf.ResolvedEventPostBuildExe)
            {
                if (customEvent is Project.Configuration.BuildStepExecutable)
                {
                    var execEvent = (Project.Configuration.BuildStepExecutable)customEvent;

                    string relativeExecutableFile = Util.PathGetRelative(conf.TargetPath, execEvent.ExecutableFile);
                    string eventString = string.Format(
                        "{0} {1}",
                        Util.SimplifyPath(envVarResolver.Resolve(relativeExecutableFile)),
                        envVarResolver.Resolve(execEvent.ExecutableOtherArguments)
                    );
                    if (!conf.EventPostBuild.Contains(eventString))
                        conf.EventPostBuild.Add(eventString);
                }
                else if (customEvent is Project.Configuration.BuildStepCopy)
                {
                    var copyEvent = (Project.Configuration.BuildStepCopy)customEvent;
                    string eventString = copyEvent.GetCopyCommand(conf.TargetPath, envVarResolver);
                    if (!conf.EventPostBuild.Contains(eventString))
                        conf.EventPostBuild.Add(eventString);
                }
                else
                {
                    throw new Error("Invalid type in PostBuild steps");
                }
            }
        }
        #endregion 

        #region Options



        private Options.ExplicitOptions GenerateOptions(CSharpProject project, Project.Configuration conf)
        {
            var options = new Options.ExplicitOptions();

            #region General

            // Default defines...
            switch (conf.Platform)
            {
                case Platform.win32:
                    options.ExplicitDefines.Add("WIN32");
                    break;
                case Platform.win64:
                    options.ExplicitDefines.Add("WIN64");
                    break;
                default:
                    break;
            }

            if (project is CSharpVstoProject)
            {
                options.ExplicitDefines.Add("VSTO40");
            }

            if (conf.DefaultOption == Options.DefaultTarget.Debug)
            {
                options.ExplicitDefines.Add("DEBUG");
                options.ExplicitDefines.Add("TRACE");
            }
            else // Release
            {
                options.ExplicitDefines.Add("TRACE");
            }
            //Output
            var simpleOutputType = Project.Configuration.SimpleOutputType(conf.Output);
            switch (simpleOutputType)
            {
                case Project.Configuration.OutputType.Exe:
                    options["ConfigurationType"] = "Application";
                    break;
                case Project.Configuration.OutputType.Dll:
                    if (conf.Platform != Platform.win32 && conf.Platform != Platform.win64 && conf.Platform != Platform.anycpu)
                        throw new Error("Only win32 and win64 platform support dll output type: {0}", conf.Target);
                    options["ConfigurationType"] = "DynamicLibrary";
                    break;
            }

            string outputDirectoryRelative = conf.PreferRelativePaths ? Util.PathGetRelative(_projectPath, conf.TargetPath) : Util.PathGetAbsolute(_projectPath, conf.TargetPath);
            string outputLibDirectoryRelative = conf.PreferRelativePaths ? Util.PathGetRelative(_projectPath, conf.TargetLibraryPath) : Util.PathGetAbsolute(_projectPath, conf.TargetLibraryPath);

            options["OutputDirectory"] = conf.Output == Project.Configuration.OutputType.Lib ? outputLibDirectoryRelative : outputDirectoryRelative;

            //IntermediateDirectory
            string intermediateDirectory = conf.PreferRelativePaths ? Util.PathGetRelative(_projectPath, conf.IntermediatePath) : Util.PathGetAbsolute(_projectPath, conf.IntermediatePath);
            options["IntermediateDirectory"] = intermediateDirectory;

            //BaseIntermediateOutputPath
            options["BaseIntermediateOutputPath"] = string.IsNullOrEmpty(conf.BaseIntermediateOutputPath) ? RemoveLineTag : Util.PathGetRelative(_projectPath, conf.BaseIntermediateOutputPath);

            options["StartWorkingDirectory"] = string.IsNullOrEmpty(conf.StartWorkingDirectory) ? RemoveLineTag : conf.StartWorkingDirectory;
            options["DocumentationFile"] = string.IsNullOrEmpty(conf.XmlDocumentationFile) ? RemoveLineTag : Util.PathGetRelative(_projectPath, conf.XmlDocumentationFile);

            ProcessDependencyCopy(project, conf);

            if (conf.EventPreBuild.Count == 0)
            {
                options["PreBuildEvent"] = RemoveLineTag;
                options["PreBuildEventDescription"] = RemoveLineTag;
                options["PreBuildEventEnable"] = RemoveLineTag;
            }
            else
            {
                options["PreBuildEvent"] = conf.EventPreBuild.JoinStrings(Environment.NewLine, escapeXml: true);
                options["PreBuildEventDescription"] = conf.EventPreBuildDescription != string.Empty ? conf.EventPreBuildDescription : RemoveLineTag;
                options["PreBuildEventEnable"] = conf.EventPreBuildExcludedFromBuild ? "false" : "true";
            }

            if (conf.EventPostBuild.Count == 0)
            {
                options["PostBuildEvent"] = RemoveLineTag;
                options["PostBuildEventDescription"] = RemoveLineTag;
                options["PostBuildEventEnable"] = RemoveLineTag;
            }
            else
            {
                options["PostBuildEvent"] = Util.JoinStrings(conf.EventPostBuild, Environment.NewLine, escapeXml: true);
                options["PostBuildEventDescription"] = conf.EventPostBuildDescription != string.Empty ? conf.EventPostBuildDescription : RemoveLineTag;
                options["PostBuildEventEnable"] = conf.EventPostBuildExcludedFromBuild ? "false" : "true";
            }


            #endregion

            SelectOption
            (
            Options.Option(Options.CSharp.CreateVsixContainer.Enabled, () => { options["CreateVsixContainer"] = "True"; }),
            Options.Option(Options.CSharp.CreateVsixContainer.Disabled, () => { options["CreateVsixContainer"] = RemoveLineTag; })
            );

            options["VsixType"] = (project.ProjectTypeGuids == CSharpProjectType.Vsix && project.VSIXProjectVersion != -1) ? string.Format("v{0}", project.VSIXProjectVersion) : RemoveLineTag;


            SelectOption
            (
            Options.Option(Options.CSharp.GeneratePkgDefFile.Enabled, () => { options["GeneratePkgDefFile"] = "True"; }),
            Options.Option(Options.CSharp.GeneratePkgDefFile.Disabled, () => { options["GeneratePkgDefFile"] = "False"; }),
            Options.Option(Options.CSharp.GeneratePkgDefFile.None, () => { options["GeneratePkgDefFile"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.IncludeAssemblyInVSIXContainer.Enabled, () => { options["IncludeAssemblyInVSIXContainer"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.IncludeAssemblyInVSIXContainer.Disabled, () => { options["IncludeAssemblyInVSIXContainer"] = "False"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.CopyVsixExtensionFiles.Enabled, () => { options["CopyVsixExtensionFiles"] = "true"; }),
            Options.Option(Options.CSharp.CopyVsixExtensionFiles.Disabled, () => { options["CopyVsixExtensionFiles"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.DeployExtension.Enabled, () => { options["DeployExtension"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.DeployExtension.Disabled, () => { options["DeployExtension"] = "False"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.DefaultConfiguration.Debug, () => { options["DefaultConfiguration"] = "Debug"; }),
            Options.Option(Options.CSharp.DefaultConfiguration.Release, () => { options["DefaultConfiguration"] = "Release"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.DebugType.Full, () => { options["DebugType"] = "full"; }),
            Options.Option(Options.CSharp.DebugType.Pdbonly, () => { options["DebugType"] = "pdbonly"; }),
            Options.Option(Options.CSharp.DebugType.Portable, () => { options["DebugType"] = "portable"; }),
            Options.Option(Options.CSharp.DebugType.Embedded, () => { options["DebugType"] = "embedded"; }),
            Options.Option(Options.CSharp.DebugType.None, () => { options["DebugType"] = "none"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.ErrorReport.Prompt, () => { options["ErrorReport"] = "prompt"; }),
            Options.Option(Options.CSharp.ErrorReport.Queue, () => { options["ErrorReport"] = "queue"; }),
            Options.Option(Options.CSharp.ErrorReport.None, () => { options["ErrorReport"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.InstallFrom.Web, () => { options["InstallFrom"] = "Web"; }),
            Options.Option(Options.CSharp.InstallFrom.Disk, () => { options["InstallFrom"] = "Disk"; }),
            Options.Option(Options.CSharp.InstallFrom.Unc, () => { options["InstallFrom"] = "Unc"; }),
            Options.Option(Options.CSharp.InstallFrom.None, () => { options["InstallFrom"] = RemoveLineTag; })
            );

            SelectOption(
            Options.Option(Options.CSharp.UpdateMode.Foreground, () => { options["UpdateMode"] = "Foreground"; }),
            Options.Option(Options.CSharp.UpdateMode.Other, () => { options["UpdateMode"] = RemoveLineTag; })
            );


            SelectOption(
            Options.Option(Options.CSharp.UpdateIntervalUnits.Days, () => { options["UpdateIntervalUnits"] = "Days"; }),
            Options.Option(Options.CSharp.UpdateIntervalUnits.None, () => { options["UpdateIntervalUnits"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.SignAssembly.Enabled, () => { options["SignAssembly"] = "true"; }),
            Options.Option(Options.CSharp.SignAssembly.Disabled, () => { options["SignAssembly"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.WarningLevel.Level0, () => { options["WarningLevel"] = "TurnOffAllWarnings"; }),
            Options.Option(Options.CSharp.WarningLevel.Level1, () => { options["WarningLevel"] = "1"; }),
            Options.Option(Options.CSharp.WarningLevel.Level2, () => { options["WarningLevel"] = "2"; }),
            Options.Option(Options.CSharp.WarningLevel.Level3, () => { options["WarningLevel"] = "3"; }),
            Options.Option(Options.CSharp.WarningLevel.Level4, () => { options["WarningLevel"] = "4"; }),
            Options.Option(Options.CSharp.WarningLevel.Level5, () => { options["WarningLevel"] = "5"; })
            );

            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version#c-language-version-reference
            SelectOption
            (
            Options.Option(Options.CSharp.LanguageVersion.LatestMajorVersion, () => { options["LanguageVersion"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.LanguageVersion.LatestMinorVersion, () => { options["LanguageVersion"] = "latest"; }),
            Options.Option(Options.CSharp.LanguageVersion.Preview, () => { options["LanguageVersion"] = "preview"; }),
            Options.Option(Options.CSharp.LanguageVersion.ISO1, () => { options["LanguageVersion"] = "ISO-1"; }),
            Options.Option(Options.CSharp.LanguageVersion.ISO2, () => { options["LanguageVersion"] = "ISO-2"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp3, () => { options["LanguageVersion"] = "3"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp4, () => { options["LanguageVersion"] = "4"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp5, () => { options["LanguageVersion"] = "5"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp6, () => { options["LanguageVersion"] = "6"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp7, () => { options["LanguageVersion"] = "7"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp7_1, () => { options["LanguageVersion"] = "7.1"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp7_2, () => { options["LanguageVersion"] = "7.2"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp7_3, () => { options["LanguageVersion"] = "7.3"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp8, () => { options["LanguageVersion"] = "8.0"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp9, () => { options["LanguageVersion"] = "9.0"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp10, () => { options["LanguageVersion"] = "10.0"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp11, () => { options["LanguageVersion"] = "11.0"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp12, () => { options["LanguageVersion"] = "12.0"; }),
            Options.Option(Options.CSharp.LanguageVersion.CSharp13, () => { options["LanguageVersion"] = "13.0"; })
            );

            SelectOption(
            Options.Option(Options.CSharp.Install.Enabled, () => { options["Install"] = "true"; }),
            Options.Option(Options.CSharp.Install.Disabled, () => { options["Install"] = RemoveLineTag; })
            );
            SelectOption(
            Options.Option(Options.CSharp.UpdateEnabled.Enabled, () => { options["UpdateEnabled"] = "true"; }),
            Options.Option(Options.CSharp.UpdateEnabled.Disabled, () => { options["UpdateEnabled"] = RemoveLineTag; })
            );
            SelectOption(
            Options.Option(Options.CSharp.UpdatePeriodically.Enabled, () => { options["UpdatePeriodically"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.UpdatePeriodically.Disabled, () => { options["UpdatePeriodically"] = "false"; })
            );

            SelectOption(
            Options.Option(Options.CSharp.UpdateRequired.Enabled, () => { options["UpdateRequired"] = "true"; }),
            Options.Option(Options.CSharp.UpdateRequired.Disabled, () => { options["UpdateRequired"] = RemoveLineTag; })
            );

            SelectOption(
            Options.Option(Options.CSharp.CopyOutputSymbolsToOutputDirectory.Enabled, () => { options["CopyOutputSymbolsToOutputDirectory"] = "true"; }),
            Options.Option(Options.CSharp.CopyOutputSymbolsToOutputDirectory.Disabled, () => { options["CopyOutputSymbolsToOutputDirectory"] = RemoveLineTag; })
            );

            SelectOption(
            Options.Option(Options.CSharp.MapFileExtensions.Enabled, () => { options["MapFileExtensions"] = "true"; }),
            Options.Option(Options.CSharp.MapFileExtensions.Disabled, () => { options["MapFileExtensions"] = RemoveLineTag; })
            );

            SelectOption(
            Options.Option(Options.CSharp.IsWebBootstrapper.Enabled, () => { options["IsWebBootstrapper"] = "true"; }),
            Options.Option(Options.CSharp.IsWebBootstrapper.Disabled, () => { options["IsWebBootstrapper"] = RemoveLineTag; })
            );

            SelectOption(
            Options.Option(Options.CSharp.PublishWizardCompleted.Enabled, () => { options["PublishWizardCompleted"] = "true"; }),
            Options.Option(Options.CSharp.PublishWizardCompleted.Disabled, () => { options["PublishWizardCompleted"] = RemoveLineTag; })
            );

            SelectOption(
            Options.Option(Options.CSharp.OpenBrowserOnPublish.Enabled, () => { options["OpenBrowserOnPublish"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.OpenBrowserOnPublish.Disabled, () => { options["OpenBrowserOnPublish"] = "false"; })
            );

            SelectOption(
            Options.Option(Options.CSharp.CreateWebPageOnPublish.Enabled, () => { options["CreateWebPageOnPublish"] = "true"; }),
            Options.Option(Options.CSharp.CreateWebPageOnPublish.Disabled, () => { options["CreateWebPageOnPublish"] = RemoveLineTag; })
            );

            SelectOption(
            Options.Option(Options.CSharp.CreateDesktopShortcut.Enabled, () => { options["CreateDesktopShortcut"] = "true"; }),
            Options.Option(Options.CSharp.CreateDesktopShortcut.Disabled, () => { options["CreateDesktopShortcut"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.UseCodeBase.Enabled, () => { options["UseCodeBase"] = "true"; }),
            Options.Option(Options.CSharp.UseCodeBase.Disabled, () => { options["UseCodeBase"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch.Enabled, () => { options["ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch.Disabled, () => { options["ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch"] = "None"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.DebugSymbols.Enabled, () => { options["DebugSymbols"] = "true"; }),
            Options.Option(Options.CSharp.DebugSymbols.Disabled, () => { options["DebugSymbols"] = "false"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.Optimize.Enabled, () => { options["Optimize"] = "true"; }),
            Options.Option(Options.CSharp.Optimize.Disabled, () => { options["Optimize"] = "false"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.AllowUnsafeBlocks.Enabled, () => { options["AllowUnsafeBlocks"] = "true"; }),
            Options.Option(Options.CSharp.AllowUnsafeBlocks.Disabled, () => { options["AllowUnsafeBlocks"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.Prefer32Bit.Enabled, () => { options["Prefer32Bit"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.Prefer32Bit.Disabled, () => { options["Prefer32Bit"] = "false"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.DisableFastUpToDateCheck.Enabled, () => { options["DisableFastUpToDateCheck"] = "true"; }),
            Options.Option(Options.CSharp.DisableFastUpToDateCheck.Disabled, () => { options["DisableFastUpToDateCheck"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.TreatWarningsAsErrors.Enabled, () => { options["TreatWarningsAsErrors"] = "true"; }),
            Options.Option(Options.CSharp.TreatWarningsAsErrors.Disabled, () => { options["TreatWarningsAsErrors"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.GenerateManifests.Enabled, () => { options["GenerateManifests"] = "true"; }),
            Options.Option(Options.CSharp.GenerateManifests.Disabled, () => { options["GenerateManifests"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.UseVSHostingProcess.Enabled, () => { options["UseVSHostingProcess"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.UseVSHostingProcess.Disabled, () => { options["UseVSHostingProcess"] = "false"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.SignManifests.Enabled, () => { options["SignManifests"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.SignManifests.Disabled, () => { options["SignManifests"] = "false"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.UseApplicationTrust.Enabled, () => { options["UseApplicationTrust"] = "true"; }),
            Options.Option(Options.CSharp.UseApplicationTrust.Disabled, () => { options["UseApplicationTrust"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.BootstrapperEnabled.Enabled, () => { options["BootstrapperEnabled"] = "true"; }),
            Options.Option(Options.CSharp.BootstrapperEnabled.Disabled, () => { options["BootstrapperEnabled"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.DllBaseAddress.x11000000, () => { options["BaseAddress"] = "285212672"; }),
            Options.Option(Options.CSharp.DllBaseAddress.x12000000, () => { options["BaseAddress"] = "301989888"; }),
            Options.Option(Options.CSharp.DllBaseAddress.None, () => { options["BaseAddress"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.FileAlignment.None, () => { options["FileAlignment"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.FileAlignment.Value512, () => { options["FileAlignment"] = "512"; }),
            Options.Option(Options.CSharp.FileAlignment.Value1024, () => { options["FileAlignment"] = "1024"; }),
            Options.Option(Options.CSharp.FileAlignment.Value2048, () => { options["FileAlignment"] = "2048"; }),
            Options.Option(Options.CSharp.FileAlignment.Value4096, () => { options["FileAlignment"] = "4096"; }),
            Options.Option(Options.CSharp.FileAlignment.Value8192, () => { options["FileAlignment"] = "8192"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.RollForward.Minor, () => { options["RollForward"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.RollForward.Major, () => { options["RollForward"] = "Major"; }),
            Options.Option(Options.CSharp.RollForward.LatestPatch, () => { options["RollForward"] = "LatestPatch"; }),
            Options.Option(Options.CSharp.RollForward.LatestMinor, () => { options["RollForward"] = "LatestMinor"; }),
            Options.Option(Options.CSharp.RollForward.LatestMajor, () => { options["RollForward"] = "LatestMajor"; }),
            Options.Option(Options.CSharp.RollForward.Disable, () => { options["RollForward"] = "Disable"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.RegisterOutputPackage.Enabled, () => { options["RegisterOutputPackage"] = "True"; }),
            Options.Option(Options.CSharp.RegisterOutputPackage.Disabled, () => { options["RegisterOutputPackage"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.RegisterWithCodebase.Enabled, () => { options["RegisterWithCodebase"] = "True"; }),
            Options.Option(Options.CSharp.RegisterWithCodebase.Disabled, () => { options["RegisterWithCodebase"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.AutoGenerateBindingRedirects.Enabled, () => { options["AutoGenerateBindingRedirects"] = "True"; }),
            Options.Option(Options.CSharp.AutoGenerateBindingRedirects.Disabled, () => { options["AutoGenerateBindingRedirects"] = RemoveLineTag; })
            );

            SelectOption
            (
                Options.Option(Options.CSharp.GenerateBindingRedirectsOutputType.Enabled, () => { options["GenerateBindingRedirectsOutputType"] = "True"; }),
                Options.Option(Options.CSharp.GenerateBindingRedirectsOutputType.Disabled, () => { options["GenerateBindingRedirectsOutputType"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.SonarQubeExclude.Disabled, () => { options["SonarQubeExclude"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.SonarQubeExclude.Enabled, () => { options["SonarQubeExclude"] = "True"; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.ProduceReferenceAssembly.Enabled, () => { options["ProduceReferenceAssembly"] = "True"; }),
            Options.Option(Options.CSharp.ProduceReferenceAssembly.Disabled, () => { options["ProduceReferenceAssembly"] = RemoveLineTag; })
            );

            SelectOption
            (
            Options.Option(Options.CSharp.IsPublishable.Enabled, () => { options["IsPublishable"] = RemoveLineTag; }),
            Options.Option(Options.CSharp.IsPublishable.Disabled, () => { options["IsPublishable"] = "false"; })
            );

            if (conf.Target.GetFragment<DotNetFramework>().IsDotNetCore())
            {
                SelectOption
                (
                Options.Option(Options.CSharp.PublishSingleFile.Enabled, () => { options["PublishSingleFile"] = "true"; }),
                Options.Option(Options.CSharp.PublishSingleFile.Disabled, () => { options["PublishSingleFile"] = RemoveLineTag; })
                );
                SelectOption
                (
                Options.Option(Options.CSharp.PublishTrimmed.Enabled, () => { options["PublishTrimmed"] = "true"; }),
                Options.Option(Options.CSharp.PublishTrimmed.Disabled, () => { options["PublishTrimmed"] = RemoveLineTag; })
                );
            }
            else
            {
                options["PublishSingleFile"] = RemoveLineTag;
                options["PublishTrimmed"] = RemoveLineTag;
            }

            options["AssemblyOriginatorKeyFile"] = Options.PathOption.Get<Options.CSharp.AssemblyOriginatorKeyFile>(conf, RemoveLineTag, _projectPath);
            options["MinimumVisualStudioVersion"] = Options.StringOption.Get<Options.CSharp.MinimumVisualStudioVersion>(conf);
            options["OldToolsVersion"] = Options.StringOption.Get<Options.CSharp.OldToolsVersion>(conf);
            options["ApplicationRevision"] = Options.StringOption.Get<Options.CSharp.ApplicationRevision>(conf);
            options["ApplicationVersion"] = Options.StringOption.Get<Options.CSharp.ApplicationVersion>(conf);
            options["VsToolsPath"] = Options.StringOption.Get<Options.CSharp.VsToolsPath>(conf);
            options["VisualStudioVersion"] = Options.StringOption.Get<Options.CSharp.VisualStudioVersion>(conf);
            options["InstallUrl"] = Options.StringOption.Get<Options.CSharp.InstallURL>(conf);
            options["SupportUrl"] = Options.StringOption.Get<Options.CSharp.SupportUrl>(conf);
            options["ProductName"] = Options.StringOption.Get<Options.CSharp.ProductName>(conf);
            options["PublisherName"] = Options.StringOption.Get<Options.CSharp.PublisherName>(conf);
            options["WebPage"] = Options.StringOption.Get<Options.CSharp.WebPage>(conf);
            options["BootstrapperComponentsUrl"] = Options.StringOption.Get<Options.CSharp.BootstrapperComponentsUrl>(conf);
            options["MinimumRequiredVersion"] = Options.StringOption.Get<Options.CSharp.MinimumRequiredVersion>(conf);
            options["NoWarn"] = Options.StringOption.Get<Options.CSharp.SuppressWarning>(conf);
            options["WarningsNotAsErrors"] = Options.StringOption.Get<Options.CSharp.WarningsNotAsErrors>(conf);
            options["WarningsAsErrors"] = Options.StringOption.Get<Options.CSharp.WarningsAsErrors>(conf);
            options["ConcordSDKDir"] = Options.StringOption.Get<Options.CSharp.ConcordSDKDir>(conf);
            options["UpdateInterval"] = Options.IntOption.Get<Options.CSharp.UpdateInterval>(conf);
            options["PublishUrl"] = Options.StringOption.Get<Options.CSharp.PublishURL>(conf);
            options["ManifestKeyFile"] = Options.StringOption.Get<Options.CSharp.ManifestKeyFile>(conf);
            options["ManifestCertificateThumbprint"] = Options.StringOption.Get<Options.CSharp.ManifestCertificateThumbprint>(conf);
            options["CopyVsixExtensionLocation"] = Options.StringOption.Get<Options.CSharp.CopyVsixExtensionLocation>(conf);
            options["ProductVersion"] = Options.StringOption.Get<Options.CSharp.ProductVersion>(conf);
            options["FileVersion"] = Options.StringOption.Get<Options.CSharp.FileVersion>(conf);
            options["Version"] = Options.StringOption.Get<Options.CSharp.Version>(conf);
            options["Product"] = Options.StringOption.Get<Options.CSharp.Product>(conf);
            options["Copyright"] = Options.StringOption.Get<Options.CSharp.Copyright>(conf);

            SelectOption
            (
                Options.Option(Options.CSharp.UseWpf.Enabled, () => { options["UseWpf"] = "true"; }),
                Options.Option(Options.CSharp.UseWpf.Disabled, () => { options["UseWpf"] = RemoveLineTag; })
            );

            SelectOption
           (
               Options.Option(Options.CSharp.PublishAot.Enabled, () => { options["PublishAot"] = "true"; }),
               Options.Option(Options.CSharp.PublishAot.Disabled, () => { options["PublishAot"] = RemoveLineTag; })
           );

            SelectOption
            (
                Options.Option(Options.CSharp.UseWindowsForms.Enabled, () => { options["UseWindowsForms"] = "true"; }),
                Options.Option(Options.CSharp.UseWindowsForms.Disabled, () => { options["UseWindowsForms"] = RemoveLineTag; })
            );

            SelectOption
            (
                Options.Option(Options.CSharp.Nullable.Enabled, () => { options["Nullable"] = "enable"; }),
                Options.Option(Options.CSharp.Nullable.Disabled, () => { options["Nullable"] = RemoveLineTag; })
            );

            // concat defines, don't add options.Defines since they are automatically added by VS
            Strings defines = new Strings();
            defines.AddRange(options.ExplicitDefines);
            defines.AddRange(conf.Defines);

            options["PreprocessorDefinitions"] = defines.JoinStrings(";").Replace(@"""", @"\&quot;");

            return options;
        }
        #endregion

        private class UserFile : UserFileBase
        {
            public UserFile(string projectFilePath) : base(projectFilePath)
            {
            }

            protected override void GenerateConfigurationContent(IFileGenerator fileGenerator, Project.Configuration conf)
            {
                using (fileGenerator.Declare("unmanagedDebugEnabled", conf.CsprojUserFile.EnableUnmanagedDebug ? "true" : FileGeneratorUtilities.RemoveLineTag))
                {
                    switch (conf.CsprojUserFile.StartAction)
                    {
                        case StartActionSetting.Program:
                            fileGenerator.WriteLine(CSproj.Template.UserFile.StartWithProgram);
                            break;
                        case StartActionSetting.URL:
                            fileGenerator.WriteLine(CSproj.Template.UserFile.StartWithUrl);
                            break;
                        default:
                            fileGenerator.WriteLine(CSproj.Template.UserFile.StartWithProject);
                            break;
                    }

                    fileGenerator.WriteLine(CSproj.Template.UserFile.DebugUnmanaged);
                }
            }

            protected override bool HasContentForConfiguration(Project.Configuration conf, out bool overwriteFile)
            {
                overwriteFile = conf.CsprojUserFile?.OverwriteExistingFile ?? true;
                return conf.CsprojUserFile != null;
            }
        }
    }
}
