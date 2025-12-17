// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "Method potiential to be reused if the IHO certificate becomes compatilble with the certificate section", Scope = "member", Target = "~M:UKHO.S100PermitService.Common.Services.KeyVaultService.GetCertificate(System.String)~System.Byte[]")]
[assembly: SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Method potiential to be reused if the IHO certificate becomes compatilble with the certificate section", Scope = "member", Target = "~M:UKHO.S100PermitService.Common.Services.KeyVaultService.GetSetCertificateValue(System.String)~System.Byte[]")]
