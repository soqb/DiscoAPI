

using System.Collections.Generic;
using DiscoAPI;
using DiscoAPI.Dialogue;
using Sunshine.Metric;
using PC = PixelCrushers.DialogueSystem;

public class ExampleProvider : DiscoProvider
{
    public string Guid => "io.github.soqb.disco-api-example-mod";

    public void OnDialogueBundleLoad(DiscoSource source)
    {
        var discoDialogue = source.manager.Disco.dialogue;

        Conversation conv = new(0, "kim-bacon-discussion", new List<Line> {
            new(0, "\"Hey Kim, I gotta tell you: I love me some bacon!\"") {
                speaker = new(discoDialogue, 396),
                links = {
                    new(source.dialogue, 0, 1)
                }
            },
            new(1, "\"That's very..\" The lieutenant sighs. \"Insightful, Detective. I like bacon too.\"") {
                speaker = new(discoDialogue, 395),
                links = {
                    new(discoDialogue, 29, 343)
                }
            },
            new(1, "Try to eat the bacon. Without opening your mouth") {
                speaker = new(discoDialogue, 410),
                node = new PassiveCheck(SkillType.PHYSICAL_INSTRUMENT, Difficulty.EXTRATRIVIAL),
                links = {
                    new(source.dialogue, 0, 2)
                }
            },
            new(2, "\"Wowee! Bacon!\"") {
                speaker = new(discoDialogue, 396),
                links = {
                    new(discoDialogue, 29, 343)
                }
            },
            new(3, "Look at Kim intently.") {
                speaker = new(discoDialogue, 396),
                node = new ActiveCheck("stare-at-kim", ActiveCheck.Kind.White, SkillType.CONCEPTUALIZATION, Difficulty.EASY),
                links = {
                    new(null, new(source.dialogue, 0, 5))
                }
            },
            new(4, "Find something *cool* to say to Kim.") {
                speaker = new(discoDialogue, 396),
                node = new ActiveCheck("be-cool-with-kim", ActiveCheck.Kind.Red, SkillType.ESPRIT_DE_CORPS, Difficulty.FORMIDABLE),
                links = {
                    new(null, new(source.dialogue, 0, 1))
                }
            },
            new(5, "Words fail you. Beyond your eyes: oblivion. Your mind is totally and completely occluded by the steaming, thick *hunk* of bacon left dripping in front of you.") {
                speaker = new(discoDialogue, 397),
                links = {
                    new(null, new(source.dialogue, 0, 1))
                }
            },
            new(6, "Kim, here's some cash.") {
                speaker = new(discoDialogue, 396),
                node = new Cost(200),
                links = {
                    new(null, new(discoDialogue, 29, 343))
                }
            },
        });
        source.dialogue.Add(conv);

        source.dialogue.InsertLink(new(
            from: new(discoDialogue, 29, 343),
            to: new(source.dialogue, 0, 0)
        ));
        source.dialogue.InsertLink(new(
            from: new(discoDialogue, 29, 343),
            to: new(source.dialogue, 0, 3)
        ));
        source.dialogue.InsertLink(new(
            from: new(discoDialogue, 29, 343),
            to: new(source.dialogue, 0, 4)
        ));
        source.dialogue.InsertLink(new(
            from: new(discoDialogue, 29, 343),
            to: new(source.dialogue, 0, 6)
        ));
    }

    public void OnSceneLoad(DiscoSource source)
    {
        source.log.LogInfo("haha! scene loaded!");
    }
}