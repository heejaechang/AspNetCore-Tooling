﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultBoundAttributeDescriptorBuilder : BoundAttributeDescriptorBuilder
    {
        private static readonly IReadOnlyDictionary<string, string> PrimitiveDisplayTypeNameLookups = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [typeof(byte).FullName] = "byte",
            [typeof(sbyte).FullName] = "sbyte",
            [typeof(int).FullName] = "int",
            [typeof(uint).FullName] = "uint",
            [typeof(short).FullName] = "short",
            [typeof(ushort).FullName] = "ushort",
            [typeof(long).FullName] = "long",
            [typeof(ulong).FullName] = "ulong",
            [typeof(float).FullName] = "float",
            [typeof(double).FullName] = "double",
            [typeof(char).FullName] = "char",
            [typeof(bool).FullName] = "bool",
            [typeof(object).FullName] = "object",
            [typeof(string).FullName] = "string",
            [typeof(decimal).FullName] = "decimal",
        };

        private readonly DefaultTagHelperDescriptorBuilder _parent;
        private readonly string _kind;
        private readonly Dictionary<string, string> _metadata;
        private List<DefaultBoundAttributeParameterDescriptorBuilder> _attributeParameterBuilders;

        private RazorDiagnosticCollection _diagnostics;

        public DefaultBoundAttributeDescriptorBuilder(DefaultTagHelperDescriptorBuilder parent, string kind)
        {
            _parent = parent;
            _kind = kind;

            _metadata = new Dictionary<string, string>();
        }

        public override string Name { get; set; }

        public override string TypeName { get; set; }

        public override bool IsEnum { get; set; }

        public override bool IsDictionary { get; set; }

        public override string IndexerAttributeNamePrefix { get; set; }

        public override string IndexerValueTypeName { get; set; }

        public override string Documentation { get; set; }

        public override string DisplayName { get; set; }

        public override IDictionary<string, string> Metadata => _metadata;

        public override RazorDiagnosticCollection Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    _diagnostics = new RazorDiagnosticCollection();
                }

                return _diagnostics;
            }
        }

        public override void BindAttributeParameter(Action<BoundAttributeParameterDescriptorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EnsureAttributeParameterBuilders();

            var builder = new DefaultBoundAttributeParameterDescriptorBuilder(this, _kind);
            configure(builder);
            _attributeParameterBuilders.Add(builder);
        }

        public BoundAttributeDescriptor Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var parameters = Array.Empty<BoundAttributeParameterDescriptor>();
            if (_attributeParameterBuilders != null)
            {
                // Attribute parameters are case-sensitive.
                var parameterset = new HashSet<BoundAttributeParameterDescriptor>(BoundAttributeParameterDescriptorComparer.CaseSensitive);
                for (var i = 0; i < _attributeParameterBuilders.Count; i++)
                {
                    parameterset.Add(_attributeParameterBuilders[i].Build());
                }

                parameters = parameterset.ToArray();
            }

            var descriptor = new DefaultBoundAttributeDescriptor(
                _kind,
                Name,
                TypeName,
                IsEnum,
                IsDictionary,
                IndexerAttributeNamePrefix,
                IndexerValueTypeName,
                Documentation,
                GetDisplayName(),
                parameters,
                new Dictionary<string, string>(Metadata),
                diagnostics.ToArray());

            return descriptor;
        }

        private string GetDisplayName()
        {
            if (DisplayName != null)
            {
                return DisplayName;
            }

            var parentTypeName = _parent.GetTypeName();
            var propertyName = this.GetPropertyName();

            if (TypeName != null &&
                propertyName != null &&
                parentTypeName != null)
            {
                // This looks like a normal c# property, so lets compute a display name based on that.
                if (!PrimitiveDisplayTypeNameLookups.TryGetValue(TypeName, out var simpleTypeName))
                {
                    simpleTypeName = TypeName;
                }

                return $"{simpleTypeName} {parentTypeName}.{propertyName}";
            }

            return Name;
        }

        private IEnumerable<RazorDiagnostic> Validate()
        {
            // data-* attributes are explicitly not implemented by user agents and are not intended for use on
            // the server; therefore it's invalid for TagHelpers to bind to them.
            const string DataDashPrefix = "data-";
            var isDirectiveAttribute = this.IsDirectiveAttribute();

            if (string.IsNullOrWhiteSpace(Name))
            {
                if (IndexerAttributeNamePrefix == null)
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeNullOrWhitespace(
                        _parent.GetDisplayName(),
                        GetDisplayName());

                    yield return diagnostic;
                }
            }
            else
            {
                if (Name.StartsWith(DataDashPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeNameStartsWith(
                        _parent.GetDisplayName(),
                        GetDisplayName(),
                        Name);

                    yield return diagnostic;
                }

                var name = Name;
                if (isDirectiveAttribute && name.StartsWith("@"))
                {
                    name = name.Substring(1);
                }
                else if (isDirectiveAttribute)
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundDirectiveAttributeName(
                            _parent.GetDisplayName(),
                            GetDisplayName(),
                            Name);

                    yield return diagnostic;
                }

                foreach (var character in name)
                {
                    if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeName(
                            _parent.GetDisplayName(),
                            GetDisplayName(),
                            name,
                            character);

                        yield return diagnostic;
                    }
                }
            }

            if (IndexerAttributeNamePrefix != null)
            {
                if (IndexerAttributeNamePrefix.StartsWith(DataDashPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributePrefixStartsWith(
                        _parent.GetDisplayName(),
                        GetDisplayName(),
                        IndexerAttributeNamePrefix);

                    yield return diagnostic;
                }
                else if (IndexerAttributeNamePrefix.Length > 0 && string.IsNullOrWhiteSpace(IndexerAttributeNamePrefix))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeNullOrWhitespace(
                        _parent.GetDisplayName(),
                        GetDisplayName());

                    yield return diagnostic;
                }
                else
                {
                    var indexerPrefix = IndexerAttributeNamePrefix;
                    if (isDirectiveAttribute && indexerPrefix.StartsWith("@"))
                    {
                        indexerPrefix = indexerPrefix.Substring(1);
                    }
                    else if (isDirectiveAttribute)
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundDirectiveAttributePrefix(
                            _parent.GetDisplayName(),
                            GetDisplayName(),
                            indexerPrefix);

                        yield return diagnostic;
                    }

                    foreach (var character in indexerPrefix)
                    {
                        if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                        {
                            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributePrefix(
                                _parent.GetDisplayName(),
                                GetDisplayName(),
                                indexerPrefix,
                                character);

                            yield return diagnostic;
                        }
                    }
                }
            }
        }

        private void EnsureAttributeParameterBuilders()
        {
            if (_attributeParameterBuilders == null)
            {
                _attributeParameterBuilders = new List<DefaultBoundAttributeParameterDescriptorBuilder>();
            }
        }
    }
}
