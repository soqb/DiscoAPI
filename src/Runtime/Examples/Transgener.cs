using System.Collections.Generic;
using DiscoAPI.Runtime;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Common.Assets;
using BepInEx;

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
public class Transgener : DiscoPlugin
{
    // This is the globally-unique identifier for the mod.
    public const string GUID = "transgener-example";

    // We override the virtual `OnDialogueBundleLoad` method which is run automatically
    // when the vanilla dialogue bundle has loaded but before it is postprocessed.
    public override void OnDialogueBundleLoad()
    {
        // We are given a `DiscoSource` based on our guid that is our interface into DE.
        // We can get out a `AssetSource` which lets us add assets into the dialogue system.

        // We create and add a new asset (in this case an actor).
        // These assets are converted and placed in the base game's dialogue database
        // which is managed by the PixelCrushers.DialogueSystem library.
        // Anything that can talk is an actor, from Tequila Sunset to the Nightwatchman's Booth.
        Actor woman = new Actor("young-trans-woman", "Young Woman");
        Assets.Add(woman);


        // We get a reference to the vanilla dialogue through the `Disco` property.
        // This lets us easily reference vanilla actors (like Empathy) and conversations.
        DiscoSource disco = Source.Manager.Disco;

        AssetRef harry = new(AssetType.Actor, disco, 396);
        AssetRef kim = new(AssetType.Actor, disco, 395);

        // We setup another asset, this time a variable to use in the conversation later.
        Variable elchemCheck = new Variable("fuck-trans-women", false);
        Assets.Add(elchemCheck);

        // We create a list of dialogue lines which we will later put into a conversation.
        Line[] lines = new[] {
            // Each line must have a numeric id which is used to reference it within the conversation.
            // We should always *try* to make them sequential, but they must only be in ascending order. 
            new Line(0, "Actually, detective, I'm a woman.") {
                // The speaker is the actor who says the line.
                // Once we've added the actor, we can turn it into an ActorRef and use it in this line here.
                speaker = woman.Ref,
                links = {
                    // `links` is a list of references to the next lines of dialogue that can be spoken.
                    // Here, we just reference the next line.
                    new Link(Source, 0, 1),
                },
            },
            new(1, "She says it so insistently, as if arguing with you. You may have upset her.") {
                // A node is any extra data that a dialogue line might need.
                // We currently support passive checks, active checks (white, red, always fail, always succeed)
                // and costs (amounts of Reál to pay).
                // If this check is failed, the game will simply not speak the line and carry on.
                //
                // The `PassiveCheck` node automatically sets the value of the speaker.  
                node = new PassiveCheck(Skills.Empathy, Difficulty.Easy),
                links = { new(Source, 0, 2) },
            },
            new(2, "You feel a pit in your stomach. You did something wrong, but you don't know what.") {
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
                links = { new(Source, 0, 3) },
            },
            new(3, "Her way of dressing, the feminine name, yet deep voice - it should have been clear to you sooner. She's transgender.") {
                node = new PassiveCheck(Skills.Logic, Difficulty.Trivial),
                links = { new(Source, 0, 4) },
            },
            new(4, "Almost imperceptible, the lieutenant anxiously twitches his eyebrow.") {
                node = new PassiveCheck(Skills.EspritDeCorps, Difficulty.Formidable),
                links = {
                    // If and only if *all* the speakers of the currently linked-to lines are Tequila Sunset
                    // (the actor with id "disco:396"), then the response menu will be shown to the player.
                    // If even one line is not spoken by the player, it will be read automatically.
                    new(Source, 0, 5),
                    new(Source, 0, 6),
                },
            },
            new(5, "Transgender? What's that?") {
                speaker = harry,
                links = { new(Source, 0, 7) },
            },
            new(6, "This doesn't have any bearing on the investigation.") {
                speaker = harry,
                links = {
                    // On dialogue options we don't care about for this *very specific* example,
                    // We just return to the beginning of the conversation.
                    new(Source, 0, 0),
                },
            },
            new(7, "A transgender person is someone who does not identify with the gender they were assigned at birth. Oftentimes they will dress conforming to their desired gender roles, change their names, and seek medical intervention to, \"transition.\"") {
                node = new PassiveCheck(Skills.Encyclopedia, Difficulty.Trivial),
                links = {
                    new(Source, 0, 8),
                    new(Source, 0, 9),
                    new(Source, 0, 10),
                    new(Source, 0, 11),
                },
            },
            new(8, "Gender is rather bourgeois, anyway.") {
                speaker = harry,
                links = { new(Source, 0, 12) },
            },
            new(9, "Why would any proud Revacholian discard their masculinity?") {
                speaker = harry,
                links = { new(Source, 0, 0) },
            },
            new(10, "Changing your gender? That sounds like quite the hustle. Maybe we can learn a thing or two from this woman.") {
                speaker = harry,
                links = { new(Source, 0, 0) },
            },
            new(11, "That's cool. I have no opinion on this one way or another.") {
                speaker = harry,
                links = { new(Source, 0, 0) },
            },
            new(12, "Just as Mazov dared to challenge the established order of capitalism, so too do others challenge the order of things such as sex and gender.") {
                node = new PassiveCheck(Skills.Rhetoric, Difficulty.Trivial),
                links = { new(Source, 0, 13) },
            },
            new(13, "IT'S BEEN SO LONG SINCE WE'VE FELT THE TOUCH OF A WOMAN. WHO CARES IF SHE USED TO BE A MAN? HAVE SEX WITH HER NOW! ITS WHAT A REAL MAN WOULD DO!") {
                node = new PassiveCheck(Skills.Electrochemistry, Difficulty.Trivial),
                // We want to skip the next few passive checks completely if this one fails.
                // So if this one succeeds, we set the value of that variable to true.
                // The string interpolation resolves to an id unique to that variable.
                script = $"SetVariableValue(\"{elchemCheck}\", true)",
                links = {
                    // We set down two links here because if the condition on line 14 fails,
                    // We will "fall through" to line 18 and skip all the checks we need to.
                    new(Source, 0, 14),
                    new(Source, 0, 18),
                },
            },
            new(14, "Don't do that. It's clear now, you upset her for accidentally calling her a man. Just apologize.") {
                // Here we set the condition, indexing into the Lua map `Variable` which contains the values of all variables.
                condition = $"Variable[\"{elchemCheck}\"]",
                node = new PassiveCheck(Skills.Empathy, Difficulty.Trivial),
                links = { new(Source, 0, 15) },
            },
            new(15, "Profusely.") {
                node = new PassiveCheck(Skills.Composure, Difficulty.Medium) {
                    speakOnFailure = true,
                },
                links = { new(Source, 0, 16) },
            },
            new(16, "It's important to be a good ally.") {
                node = new PassiveCheck(Skills.EspritDeCorps, Difficulty.Medium),
                links = { new(Source, 0, 17) },
            },
            new(17, "Make a real show of it, sire!") {
                node = new PassiveCheck(Skills.Drama, Difficulty.Medium),
                links = { new(Source, 0, 18) },
            },
            // In order for this line to act as a "hub", a landing page for other lines
            // that line 13 can link to, we give it no actual content.
            new(18, null) {
                speaker = new(AssetType.Actor, disco, 401),
                links = {
                    new(Source, 0, 19),
                    new(Source, 0, 20),
                    new(Source, 0, 21),
                    new(Source, 0, 22),
                },
            },
            new(19, "\"Oh, I didn't realize. I'm sorry.\"") {
                speaker = harry,
                links = { new(Source, 0, 0) },
            },
            new(20, "\"I'm so sorry I'm so sorry I'll leave you alone forever now.\"") {
                speaker = harry,
                links = { new(Source, 0, 0) },
            },
            new(21, "\"I haven't been a good representative of the RCM. We're here to help the people of Martinaise, no matter their identity. I'm sorry to have let you down.\"") {
                speaker = harry,
                links = { new(Source, 0, 0) },
            },
            new(22, "Try and come up with an elaborate, heartfelt apology in the style of the turn of the century thespians.") {
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
                links = { new(Source, 0, 23) },
            },
            new(23, "You try and come up with the words to convey your apology to the young woman, but you come up blank. It's hard to fit \"transgender\" into iambic pentameter, as it turns out.") {
                // disco:401 is the id for Drama.
                speaker = new(AssetType.Actor, disco, 401),
                links = {
                    new(Source, 0, 24),
                    new(Source, 0, 26),
                },
            },
            new(24, "\"Detective? You've been standing there for a whole minute. Are you okay?\"") {
                // Conversations can be between as many people as you like!
                // This is Kim, for example.
                speaker = kim,
                condition = "IsKimHere()",
                links = { new(Source, 0, 25) },
            },
            new(25, "Shit, the lieutenant is onto us. We have to say something soon, or we could lose him.") {
                speaker = new(AssetType.Actor, disco, 408),
                links = { new(Source, 0, 26) },
            },
            new(26, "Don't worry, we can still salvage this. Anyone have any ideas?") {
                node = new PassiveCheck(Skills.Composure, Difficulty.Trivial),
                links = { new(Source, 0, 27) },
            },
            new(27, "Let me handle this.") {
                node = new PassiveCheck(Skills.Volition, Difficulty.Heroic) {
                    speakOnFailure = true,
                },
                links = {
                    new(Source, 0, 28),
                },
            },
            new(28, "I'm so sorry, I'm so fucking sorry. I'm such a fucking failure. Do you want me to kill myself?") {
                speaker = harry,
                links = {
                    new(Source, 0, 29),
                },
            },
            new(29, null) {
                script = "NewspaperEndgame(\"HARDIES_SUICIDE\",\"DERANGED COP KILLS HIMSELF\",\"This came as a surprise to absolutely no-one. Literally what the fuck were you expecting.\") "
            },
            new(30, "Hey Kim! There's a young man over there. Wierd that we can't see them.") {
                speaker = harry,
                links = {
                    new(Source, 0, 0),
                }
            }
        };

        // We add the conversation to the source as with other assets.
        Assets.Add(new Conversation("young-woman-is-transgender", new List<Line>(lines)));

        // FIXME: BROKEN !!!
        // // And, finally, we insert a link (between conversations) to allow it to be spoken.
        // // The `from` line is the root of Kim's main dialogue tree.
        // trans.InsertLink(new Link(
        //     from: new(disco, 29, 343),
        //     to: new(trans, 0, 30)
        // ));
    }
}
