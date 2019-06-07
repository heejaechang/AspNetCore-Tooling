// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentSplatLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run after component lowering pass
        public override int Order => 50;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if (!IsComponentDocument(documentNode))
            {
                return;
            }

            var references = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>();
            var parents = new HashSet<IntermediateNode>();
            for (var i = 0; i < references.Count; i++)
            {
                parents.Add(references[i].Parent);
            }

            for (var i = 0; i < references.Count; i++)
            {
                var reference = references[i];
                var node = (TagHelperDirectiveAttributeIntermediateNode)reference.Node;

                if (!reference.Parent.Children.Contains(node))
                {
                    // This node was removed as a duplicate, skip it.
                    continue;
                }

                if (node.TagHelper.IsSplatTagHelper())
                {
                    reference.Replace(RewriteUsage(reference.Parent, node));
                }
            }
        }

        private IntermediateNode RewriteUsage(IntermediateNode parent, TagHelperDirectiveAttributeIntermediateNode node)
        {
            if (parent is ComponentIntermediateNode)
            {
                var result = new ComponentAttributeIntermediateNode()
                {
                    IsAttributeSplat = true,
                    Source = node.Source,
                    TypeName = ComponentsApi.AddMultipleAttributesTypeFullName, // Needed for type-inference cases
                };

                // For a component, the children are already structured correctly.
                result.Children.AddRange(node.Children);
                result.Diagnostics.AddRange(node.Diagnostics);

                return result;
            }
            else
            {
                var result = new HtmlAttributeIntermediateNode()
                {
                    IsAttributeSplat = true,
                    Source = node.Source,
                };

                // For an element, we need to rewrite the expression into an attribute value.
                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token)
                    {
                        var value = new CSharpExpressionAttributeValueIntermediateNode();
                        value.Children.Add(token);
                        result.Children.Add(value);
                    }
                    else if (node.Children[i] is CSharpExpressionIntermediateNode expression)
                    {
                        var value = new CSharpExpressionAttributeValueIntermediateNode();
                        value.Children.AddRange(expression.Children);
                        result.Children.Add(value);
                    }
                    else
                    {
                        // If we end up seeing something else here, just add it. This will help with
                        // diagnosability.
                        result.Children.Add(node.Children[i]);
                    }
                }

                result.Diagnostics.AddRange(node.Diagnostics);
                return result;
            }
        }
    }
}
