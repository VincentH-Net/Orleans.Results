// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0005:Using directive is unnecessary.", Justification = "This project includes shared source files that are also used outside the context of this project's global usings")]
[assembly: SuppressMessage("Naming", "CA1721:Property names should not match get methods", Justification = "Type is for tool access only", Scope = "member", Target = "~M:Example.UsersController.GetUser(System.Int32)~System.Threading.Tasks.Task{Microsoft.AspNetCore.Mvc.ActionResult{System.String}}")]
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Not relevant for ASP.NET Core / Orleans applications")]
