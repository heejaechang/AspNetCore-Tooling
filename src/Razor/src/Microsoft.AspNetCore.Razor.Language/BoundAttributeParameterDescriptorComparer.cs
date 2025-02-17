﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class BoundAttributeParameterDescriptorComparer : IEqualityComparer<BoundAttributeParameterDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="BoundAttributeParameterDescriptorComparer"/>.
        /// </summary>
        public static readonly BoundAttributeParameterDescriptorComparer Default = new BoundAttributeParameterDescriptorComparer();

        /// <summary>
        /// A default instance of the <see cref="BoundAttributeParameterDescriptorComparer"/> that does case-sensitive comparison.
        /// </summary>
        internal static readonly BoundAttributeParameterDescriptorComparer CaseSensitive =
            new BoundAttributeParameterDescriptorComparer(caseSensitive: true);

        private readonly StringComparer _stringComparer;
        private readonly StringComparison _stringComparison;

        private BoundAttributeParameterDescriptorComparer(bool caseSensitive = false)
        {
            if (caseSensitive)
            {
                _stringComparer = StringComparer.Ordinal;
                _stringComparison = StringComparison.Ordinal;
            }
            else
            {
                _stringComparer = StringComparer.OrdinalIgnoreCase;
                _stringComparison = StringComparison.OrdinalIgnoreCase;
            }
        }

        public virtual bool Equals(BoundAttributeParameterDescriptor descriptorX, BoundAttributeParameterDescriptor descriptorY)
        {
            if (object.ReferenceEquals(descriptorX, descriptorY))
            {
                return true;
            }

            if (descriptorX == null ^ descriptorY == null)
            {
                return false;
            }

            return
                string.Equals(descriptorX.Kind, descriptorY.Kind, StringComparison.Ordinal) &&
                descriptorX.IsEnum == descriptorY.IsEnum &&
                string.Equals(descriptorX.Name, descriptorY.Name, _stringComparison) &&
                string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.Documentation, descriptorY.Documentation, StringComparison.Ordinal) &&
                string.Equals(descriptorX.DisplayName, descriptorY.DisplayName, StringComparison.Ordinal) &&
                Enumerable.SequenceEqual(
                    descriptorX.Metadata.OrderBy(propertyX => propertyX.Key, StringComparer.Ordinal),
                    descriptorY.Metadata.OrderBy(propertyY => propertyY.Key, StringComparer.Ordinal));
        }

        public virtual int GetHashCode(BoundAttributeParameterDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var hash = HashCodeCombiner.Start();
            hash.Add(descriptor.Kind);
            hash.Add(descriptor.Name, _stringComparer);

            return hash.CombinedHash;
        }
    }
}
