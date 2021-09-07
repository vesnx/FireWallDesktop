using System.Reflection;
//[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]
//password that is used to decrypt the stack using  https://www.gapotchenko.com/eazfuscator
[assembly: Obfuscation(Feature = "encrypt symbol names with password qmueojFZB8zDFpGgqllanJmYDa0HQiaJT7dpiwu0L8yJb0aJZRLNE7iGjVpNr9w6", Exclude = false)]
[assembly: Obfuscation(Feature = "encrypt resources [compress]", Exclude = false)]
[assembly: Obfuscation(Feature = "ignore InternalsVisibleToAttribute", Exclude = false)]
[assembly: Obfuscation(Feature = "debug [relative-file-paths]", Exclude = false)]
[assembly: Obfuscation(Feature = "debug [secure]", Exclude = false)]
[assembly: Obfuscation(Feature = "code control flow obfuscation", Exclude = false)]
[assembly: Obfuscation(Feature = "design-time usage protection [arguments=keep]", Exclude = false)]
[assembly: Obfuscation(Feature = "Apply to type * when public: renaming", Exclude = true, ApplyToMembers = false)]