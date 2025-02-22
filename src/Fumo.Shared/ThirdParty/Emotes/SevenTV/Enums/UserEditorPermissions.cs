namespace Fumo.Shared.ThirdParty.Emotes.SevenTV.Enums;

// https://github.com/SevenTV/SevenTV/blob/main/shared/src/old_types/mod.rs#L310
[Flags]
public enum UserEditorPermissions
{
    None = 0,
    Default = ModifyEmotes | ManageEmoteSets,

    ModifyEmotes = 1 << 0,
    UsePrivateEmotes = 1 << 1,
    ManageProfile = 1 << 2,
    ManageOwnedEmotes = 1 << 3,
    ManageEmoteSets = 1 << 4,
    ManageBilling = 1 << 5,
    ManageEditors = 1 << 6,
    ViewMessages = 1 << 7,
}
