using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Assets;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Utils;

public class ModifierUtils
{
    public static EnumArena<SM.EffectType, CharacterEffect> Effects => (EnumArena<SM.EffectType, CharacterEffect>)DiscoRunner.manager.Assets.GetArena<CharacterEffect>();
    
    public static CharacterEffect? Lookup(SM.EffectType et) => Effects[Effects.ReverseId(et)];

    public static bool EffectIsVanilla(SM.EffectType type) => (int)type <= Common.Assets.Effects.VANILLA_MAX;
    public static CharacterEffect RecoverEffect(SM.EffectType type)
    {
        return new CharacterEffect(FormatUtils.Slugify(type.ToString()), null, null);
    }

    public static bool EffectIsReal(SM.EffectType type) => type switch
    {
        SM.EffectType.NONE => false,
        SM.EffectType.COMMUNISM_XP_BONUS => false,
        SM.EffectType.DRUGS_ARE_BAD_MKAY => false,
        _ => true
    };

}