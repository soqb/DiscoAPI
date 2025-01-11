using System.Collections.Generic;
using DiscoAPI.Runtime;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Common.Assets;
using BepInEx;
using BepInEx.Unity.IL2CPP;

// An example blatantly ripped from a blog by `thedeliaishere` on tumblr:
// (https://www.tumblr.com/thedeliaishere/721024362812211200/young-woman-actually-detective-im-a?source=share).

// We implement the DiscoProvider class in order to register mods.
[BepInPlugin(
    GUID,
    "transgener example",
    "0.0.1"
)]
[BepInDependency(DiscoAPIPlugin.GUID)]
[BepInProcess("disco.exe")]
public class Transgener : BasePlugin
{
    // This is the globally-unique identifier for the mod.
    public const string GUID = "transgener-example";

    // we also need to define and initialize a `DiscoSource`, our interface with the base game.
    public DiscoSource source;
    public Transgener()
    {
        source = DiscoRunner.SourceFromPlugin(this);
    }

    public override void Load()
    {
        DiscoHooks.OnDialogueLoad += OnDialogueBundleLoad;
    }

    // We override the virtual `OnDialogueBundleLoad` method which is run automatically
    // when the vanilla dialogue bundle has loaded but before it is postprocessed.
    public void OnDialogueBundleLoad()
    {
        // We create an asset to represent our speaker.
        // Dialogue assets (like this actor) are converted and placed in the base game's dialogue database
        // which is managed by the PixelCrushers.DialogueSystem library.
        // Anything that can talk is an actor, from Tequila Sunset to the Nightwatchman's Booth.
        Actor woman = new Actor("young-trans-woman", "Young Woman");

        // Now, we can register the asset so it will be properly interned and marshalled.
        // We can use the `source` property which we are given based on our guid as our interface into DE.
        // We can get out an `AssetSource` which lets us add assets into the dialogue system.
        source.Add(woman);

        // We get a reference to the vanilla dialogue through the `Disco` property.
        // This lets us easily reference vanilla actors (like Empathy) and conversations.
        DiscoSource disco = source.Manager.Disco;

        // `AssetLocation`s are a kind of `IAssetRef`:
        // * `IAssetRef` is an interface which encapsulate runtime asset resolution.
        // * Assets are themselves asset references (which resolve to themselves).
        // * `AssetLocation` is another kind of asset reference, which use dynamic lookup.
        // * Both `IAssetRef` and `AssetLocation` come in both generic (as below) and non-generic forms.
        AssetLocation<Actor> harry = new(disco, "you");
        AssetLocation<Actor> kim = new(disco, "kim-kitsuragi");

        // We setup another asset, this time a variable, to use in the following conversation.
        Variable elchemCheck = new Variable("fuck-trans-women", false);
        // Here we use the `Assets` property, which is a shorthand for `source.Assets`.
        source.Add(elchemCheck);

        LineRef line(int id) => new(source, "young-woman-is-transgender", id);

        // We create a list of dialogue lines which we will later put into a conversation.
        Line[] lines = new[] {
            // Each line must have some (nullable) text, but there's also a whole bunch of additional properties. 
            new Line("Hey Kim! There's a young man over there. Wierd that we can't see them.") {
                speaker = harry,
                links = {
                    line(1),
                }
            },
            new("Actually, detective, I'm a woman.") {
                // The speaker is the actor who says the line.
                // Since we've added an actor, we can use them in this line here.
                //
                // The `speaker` field expects an `IAssetRef<Actor>` but since assets *are* asset references,
                // we can just use `woman`.
                speaker = woman,
                links = {
                    // `links` is a list of references to the next lines of dialogue that can be spoken.
                    // Here, we just reference the next line.
                    line(2),
                },
            },
            new("She says it so insistently, as if arguing with you. You may have upset her.") {
                // A node is any extra data that a dialogue line might need.
                // We currently support passive checks, active checks (white, red, always fail, always succeed)
                // and costs (amounts of Reál to pay).
                // If this check is failed, the game will simply not speak the line and carry on.
                //
                // The `PassiveCheck` node automatically sets the value of the speaker.  
                node = new PassiveCheck(Skills.Empathy, Difficulty.Easy),
                links = { line(3) },
            },
            new("You feel a pit in your stomach. You did something wrong, but you don't know what.") {
                // We can also give passive checks the additonal `speakOnFailure = true` property,
                // which dictates that they will be spoken when the check is failed.
                // If you want separate success and fail dialogue, you will need two separate lines.
                node = new PassiveCheck(Skills.Composure, Difficulty.Formidable) {
                    speakOnFailure = true,
                },
                // We can also give lines a script to run when spoken.
                // This is written in the Lua language and uses a custom list of functions.
                // FAYDE.co.uk is a good resource for finding out how these functions are used.
                script = "DamageVolition(1)",
                links = { line(4) },
            },
            new("Her way of dressing, the feminine name, yet deep voice - it should have been clear to you sooner. She's transgender.") {
                node = new PassiveCheck(Skills.Logic, Difficulty.Trivial),
                links = { line(5) },
            },
            new("Almost imperceptible, the lieutenant anxiously twitches his eyebrow.") {
                node = new PassiveCheck(Skills.EspritDeCorps, Difficulty.Formidable),
                links = {
                    // If and only if *all* the speakers of the currently linked-to lines are Tequila Sunset
                    // (the actor with id "disco:396"), then the response menu will be shown to the player.
                    // If even one line is not spoken by the player, it will be read automatically.
                    line(6),
                    line(7),
                },
            },
            new("Transgender? What's that?") {
                speaker = harry,
                links = { line(8) },
            },
            new("This doesn't have any bearing on the investigation.") {
                speaker = harry,
                links = {
                    // On dialogue options we don't care about for this *very specific* example,
                    // We just return to the beginning of the conversation.
                    line(0),
                },
            },
            new("A transgender person is someone who does not identify with the gender they were assigned at birth. Oftentimes they will dress conforming to their desired gender roles, change their names, and seek medical intervention to, \"transition.\"") {
                node = new PassiveCheck(Skills.Encyclopedia, Difficulty.Trivial),
                links = {
                    line(9),
                    line(10),
                    line(11),
                    line(12),
                },
            },
            new("Gender is rather bourgeois, anyway.") {
                speaker = harry,
                links = { line(13) },
            },
            new("Why would any proud Revacholian discard their masculinity?") {
                speaker = harry,
                links = { line(0) },
            },
            new("Changing your gender? That sounds like quite the hustle. Maybe we can learn a thing or two from this woman.") {
                speaker = harry,
                links = { line(0) },
            },
            new("That's cool. I have no opinion on this one way or another.") {
                speaker = harry,
                links = { line(0) },
            },
            new("Just as Mazov dared to challenge the established order of capitalism, so too do others challenge the order of things such as sex and gender.") {
                node = new PassiveCheck(Skills.Rhetoric, Difficulty.Trivial),
                links = { line(14) },
            },
            new("IT'S BEEN SO LONG SINCE WE'VE FELT THE TOUCH OF A WOMAN. WHO CARES IF SHE USED TO BE A MAN? HAVE SEX WITH HER NOW! ITS WHAT A REAL MAN WOULD DO!") {
                node = new PassiveCheck(Skills.Electrochemistry, Difficulty.Trivial),
                // We want to skip the next few passive checks completely if this one fails.
                // So if this one succeeds, we set the value of that variable to true.
                // The string interpolation resolves to an id unique to that variable.
                script = $"SetVariableValue(\"{elchemCheck}\", true)",
                links = {
                    // We set down two links here because if the condition on line 14 fails,
                    // We will "fall through" to line 18 and skip all the checks we need to.
                    line(15),
                    line(19),
                },
            },
            new("Don't do that. It's clear now, you upset her for accidentally calling her a man. Just apologize.") {
                // Here we set the condition, indexing into the Lua map `Variable` which contains the values of all variables.
                condition = $"Variable[\"{elchemCheck}\"]",
                node = new PassiveCheck(Skills.Empathy, Difficulty.Trivial),
                links = { line(16) },
            },
            new("Profusely.") {
                node = new PassiveCheck(Skills.Composure, Difficulty.Medium) {
                    speakOnFailure = true,
                },
                links = { line(17) },
            },
            new("It's important to be a good ally.") {
                node = new PassiveCheck(Skills.EspritDeCorps, Difficulty.Medium),
                links = { line(18) },
            },
            new("Make a real show of it, sire!") {
                node = new PassiveCheck(Skills.Drama, Difficulty.Medium),
                links = { line(19) },
            },
            // In order for this line to act as a "hub", a landing page for other lines
            // that line 13 can link to, we give it no actual content.
            new(null) {
                speaker = new AssetLocation<Actor>(disco, 401),
                links = {
                    line(20),
                    line(21),
                    line(22),
                    line(23),
                },
            },
            new("\"Oh, I didn't realize. I'm sorry.\"") {
                speaker = harry,
                links = { line(0) },
            },
            new("\"I'm so sorry I'm so sorry I'll leave you alone forever now.\"") {
                speaker = harry,
                links = { line(0) },
            },
            new("\"I haven't been a good representative of the RCM. We're here to help the people of Martinaise, no matter their identity. I'm sorry to have let you down.\"") {
                speaker = harry,
                links = { line(0) },
            },
            new("Try and come up with an elaborate, heartfelt apology in the style of the turn of the century thespians.") {
                speaker = harry,
                // `ActiveCheck` is another of the aforementioned nodes.
                // Unlike `PassiveCheck`, the response dialogue comes in the next line.
                // The game will set the value of the variable "{guid}.checks.{checkName}" to whether it succeeded,
                // which is used to show two different lines of dialogue.
                // Since this is an "always fail" check, we don't have to do that here.
                node = new ActiveCheck(
                    "apologize-to-young-woman",
                    ActiveCheck.Kind.AlwaysFail,
                    Skills.Drama,
                    Difficulty.Legendary
                ),
                links = { line(24) },
            },
            new("You try and come up with the words to convey your apology to the young woman, but you come up blank. It's hard to fit \"transgender\" into iambic pentameter, as it turns out.") {
                speaker = new AssetLocation<Actor>(disco, "drama"),
                links = {
                    line(25),
                    line(27),
                },
            },
            new("\"Detective? You've been standing there for a whole minute. Are you okay?\"") {
                // Conversations can be between as many people as you like!
                // We use kim here, for example.
                speaker = kim,
                condition = "IsKimHere()",
                links = { line(26) },
            },
            new("Shit, the lieutenant is onto us. We have to say something soon, or we could lose him.") {
                speaker = new AssetLocation<Actor>(disco, "drama"),
                links = { line(27) },
            },
            new("Don't worry, we can still salvage this. Anyone have any ideas?") {
                node = new PassiveCheck(Skills.Composure, Difficulty.Trivial),
                links = { line(28) },
            },
            new("Let me handle this.") {
                node = new PassiveCheck(Skills.Volition, Difficulty.Heroic) {
                    speakOnFailure = true,
                },
                links = {
                    line(29),
                },
            },
            new("I'm so sorry, I'm so fucking sorry. I'm such a fucking failure. Do you want me to kill myself?") {
                speaker = harry,
                links = {
                    line(30),
                },
            },
            new(null) {
                script = "NewspaperEndgame(\"HARDIES_SUICIDE\",\"DERANGED COP KILLS HIMSELF\",\"\") "
            },
        };

        // We add the conversation to the source as with other assets.
        source.Add(new Conversation("young-woman-is-transgender", new List<Line>(lines)));

        // And, finally, we insert a link (between conversations) to allow it to be spoken.
        // The `from` line is the root of Kim's main dialogue tree.
        source.InsertLink(new Link(
            from: new(disco, "29", 343),
            to: new(source, "young-woman-is-transgender", 0)
        ));
    }
}
