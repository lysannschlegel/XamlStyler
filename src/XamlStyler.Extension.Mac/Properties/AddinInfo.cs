﻿using Mono.Addins;
using Mono.Addins.Description;

[assembly: Addin("XamlStyler", Namespace = "Xavalon", Version = "2.0.1")]

[assembly: AddinName("XAML Styler (PDX)")]
[assembly: AddinCategory("IDE extensions")]
[assembly: AddinDescription("XAML Styler is a visual studio extension that formats XAML source code based on a set of styling rules. This tool can help you/your team maintain a better XAML coding style as well as a much better XAML readability.")]
[assembly: AddinAuthor("Xavalon")]
[assembly: AddinUrl("https://github.com/Xavalon/XamlStyler/")]

[assembly: AddinDependency("::MonoDevelop.Core", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency("::MonoDevelop.Ide", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency("::MonoDevelop.TextEditor", MonoDevelop.BuildInfo.Version)]