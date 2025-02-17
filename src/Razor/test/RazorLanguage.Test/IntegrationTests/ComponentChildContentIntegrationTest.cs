// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class ComponentChildContentIntegrationTest : RazorIntegrationTestBase
    {
        private readonly CSharpSyntaxTree RenderChildContentComponent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
namespace Test
{
    public class RenderChildContent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent);
        }

        [Parameter]
        RenderFragment ChildContent { get; set; }
    }
}
");

        private readonly CSharpSyntaxTree RenderChildContentStringComponent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
namespace Test
{
    public class RenderChildContentString : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent, Value);
        }

        [Parameter]
        RenderFragment<string> ChildContent { get; set; }

        [Parameter]
        string Value { get; set; }
    }
}
");

        private readonly CSharpSyntaxTree RenderMultipleChildContent = Parse(@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
namespace Test
{
    public class RenderMultipleChildContent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Header, Name);
            builder.AddContent(1, ChildContent, Value);
            builder.AddContent(2, Footer);
        }

        [Parameter]
        string Name { get; set; }

        [Parameter]
        RenderFragment<string> Header { get; set; }

        [Parameter]
        RenderFragment<string> ChildContent { get; set; }

        [Parameter]
        RenderFragment Footer { get; set; }

        [Parameter]
        string Value { get; set; }
    }
}
");

        internal override string FileKind => FileKinds.Component;

        internal override bool UseTwoPhaseCompilation => true;

        [Fact]
        public void ChildContent_AttributeAndBody_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
@{ RenderFragment<string> template = @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template.WithValue(""HI"")"">
Some Content
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentSetByAttributeAndBody.Id, diagnostic.Id);
        }

        [Fact]
        public void ChildContent_AttributeAndExplicitChildContent_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
@{ RenderFragment<string> template = @<div>@context.ToLowerInvariant()</div>; }
<RenderChildContent ChildContent=""@template.WithValue(""HI"")"">
<ChildContent>
Some Content
</ChildContent>
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentSetByAttributeAndBody.Id, diagnostic.Id);
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_UnrecogizedContent_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContent>
<ChildContent>
</ChildContent>
@somethingElse
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentMixedWithExplicitChildContent.Id, diagnostic.Id);
            Assert.Equal(
                "Unrecognized child content inside component 'RenderChildContent'. The component 'RenderChildContent' accepts " +
                "child content through the following top-level items: 'ChildContent'.",
                diagnostic.GetMessage());
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_UnrecogizedElement_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContent>
<ChildContent>
</ChildContent>
<UnrecognizedChildContent></UnrecognizedChildContent>
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentMixedWithExplicitChildContent.Id, diagnostic.Id);
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_UnrecogizedAttribute_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContent>
<ChildContent attr>
</ChildContent>
</RenderChildContent>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentHasInvalidAttribute.Id, diagnostic.Id);
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_InvalidParameterName_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContentString>
<ChildContent Context=""@(""HI"")"">
</ChildContent>
</RenderChildContentString>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentHasInvalidParameter.Id, diagnostic.Id);
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_RepeatedParameterName_GeneratesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContentString>
<ChildContent>
<RenderChildContentString>
<ChildContent Context=""context"">
</ChildContent>
</RenderChildContentString>
</ChildContent>
</RenderChildContentString>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentRepeatedParameterName.Id, diagnostic.Id);
            Assert.Equal(
                "The child content element 'ChildContent' of component 'RenderChildContentString' uses the same parameter name ('context') as enclosing child content " +
                "element 'ChildContent' of component 'RenderChildContentString'. Specify the parameter name like: '<ChildContent Context=\"another_name\"> to resolve the ambiguity",
                diagnostic.GetMessage());
        }

        [Fact]
        public void ChildContent_ContextParameterNameOnComponent_Invalid_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContentString Context=""@Foo()"">
</RenderChildContentString>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentHasInvalidParameterOnComponent.Id, diagnostic.Id);
            Assert.Equal(
                "Invalid parameter name. The parameter name attribute 'Context' on component 'RenderChildContentString' can only include literal text.",
                diagnostic.GetMessage());
        }

        [Fact]
        public void ChildContent_ExplicitChildContent_ContainsDirectiveAttribute_ProducesDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(RenderChildContentStringComponent);

            // Act
            var generated = CompileToCSharp(@"
<RenderChildContentString>
<ChildContent Context=""items"" @key=""Hello"">
</ChildContent>
</RenderChildContentString>");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Same(ComponentDiagnosticFactory.ChildContentHasInvalidAttribute.Id, diagnostic.Id);
            Assert.Equal(
                "Unrecognized attribute '@key' on child content element 'ChildContent'.",
                diagnostic.GetMessage());
        }
    }
}
