// <auto-generated/>
#pragma warning disable 1591
namespace Test
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    public class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.RenderTree.RenderTreeBuilder builder)
        {
            builder.OpenComponent<Test.MyComponent>(0);
            builder.AddAttribute(1, "AttributeBefore", "before");
            builder.AddMultipleAttributes(2, 
#nullable restore
#line 1 "x:\dir\subdir\Test\TestComponent.cshtml"
                                                    someAttributes

#line default
#line hidden
#nullable disable
            );
            builder.AddAttribute(3, "AttributeAfter", "after");
            builder.CloseComponent();
        }
        #pragma warning restore 1998
#nullable restore
#line 3 "x:\dir\subdir\Test\TestComponent.cshtml"
       
    private Dictionary<string, object> someAttributes = new Dictionary<string, object>();

#line default
#line hidden
#nullable disable
    }
}
#pragma warning restore 1591
