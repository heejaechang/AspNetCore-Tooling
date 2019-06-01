// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

            var references = documentNode.FindDescendantReferences<TagHelperPropertyIntermediateNode>();
            for (var i = 0; i < references.Count; i++)
            {
                var reference = references[i];
                var node = (TagHelperPropertyIntermediateNode)reference.Node;

                if (node.TagHelper.IsSplatTagHelper() && node.IsDirectiveAttribute)
                {
                    reference.Replace(RewriteUsage(reference.Parent, node));
                }
            }
        }

        private IntermediateNode RewriteUsage(IntermediateNode parent, TagHelperPropertyIntermediateNode node)
        {
            if (parent is ComponentIntermediateNode)
            {
                var result = new ComponentAttributeIntermediateNode()
                {
                    IsAttributeSplat = true,
                    Source = node.Source,
                    TypeName = ComponentsApi.AddMultipleAttributesTypeFullName, // Needed for type-inference cases
                };

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
