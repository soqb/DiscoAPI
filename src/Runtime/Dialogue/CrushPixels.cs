using System.Collections.Generic;
using DiscoAPI.Common.Dialogue;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime.Dialogue;

public class DiscoToPixels
{
	DialogueSource source;
	DiscoManager Manager => source.Parent.Manager;

	public DiscoToPixels(DialogueSource source)
	{
		this.source = source;
	}

	public PC.Link Crush(Link link)
	{
		var pcLink = new PC.Link();
		pcLink.isConnector = true;
		if (link.from != null)
		{
			pcLink.originConversationID = link.from.conversation.CrushedId(Manager);
			pcLink.originDialogueID = link.from.lineID;
		}
		pcLink.destinationConversationID = link.to.conversation.CrushedId(Manager);
		pcLink.destinationDialogueID = link.to.lineID;

		return pcLink;
	}
	public PC.DialogueEntry Crush(Line line, AssetRef parentConv)
	{
		var pcEntry = new PC.DialogueEntry();
		pcEntry.conversationID = parentConv.CrushedId(Manager);
		pcEntry.id = line.internalID;
		pcEntry.fields = new();
		pcEntry.conditionsString = line.condition ?? "";
		pcEntry.userScript = line.script ?? "";
		pcEntry.ActorID = line.speaker?.CrushedId(Manager) ?? 0;
		// NB: currentDialogueText is usually the same, *except* for the fact it won't create a new field when necessary...
		pcEntry.DialogueText = line.text;
		pcEntry.Title = line.title ?? line.text ?? $"{source.Guid}:{parentConv.id}#{line.internalID}";
		if (line.sequence != null) pcEntry.Sequence = line.sequence;
		if (line.sequence != null) pcEntry.ResponseMenuSequence = line.menuSequence;
		foreach (var link in line.links)
			pcEntry.outgoingLinks.Add(Crush(link));


		if (line.node != null)
		{
			var fields = new FieldEditor(pcEntry.Title, pcEntry.fields);
			fields.Set("AlwaysPlayVoice", false);
			fields.Set("PlayVoiceInPsychologicalMode", false);

			EditFieldsForDialogue editor = new(fields, source, parentConv);
			line.node.Visit(editor);
		}

		return pcEntry;
	}

	public PC.Conversation Crush(Conversation conv)
	{
		var pcConv = new PC.Conversation();
		pcConv.fields = new();
		pcConv.Name = conv.id;
		pcConv.Title = conv.id;
		pcConv.dialogueEntries = new();

		var handle = conv.Ref;
		foreach (var line in conv.lines)
		{
			pcConv.dialogueEntries.Add(Crush(line, handle));
		}


		return pcConv;
	}

	public PC.Actor Crush(Actor actor)
	{
		var pcActor = new PC.Actor();
		pcActor.fields = new();
		pcActor.Name = actor.displayName;

		return pcActor;
	}

	public PC.Variable Crush(Variable var)
	{
		var pcVar = new PC.Variable();
		pcVar.fields = new();
		pcVar.Name = $"{source.Guid}.{var.id}";
		pcVar.InitialValue = var.initialValue;
		pcVar.Description = ""; // TODO
		pcVar.Type = (PC.FieldType)var.type;

		return pcVar;
	}
}

public class EditFieldsForDialogue : DialogueNodeVisitor
{
	public FieldEditor fields;
	public DialogueSource source;
	public AssetRef parentConv;

	public EditFieldsForDialogue(FieldEditor fields, DialogueSource source, AssetRef parentConv)
	{
		this.fields = fields;
		this.source = source;
		this.parentConv = parentConv;
	}

	public void Active(ActiveCheck ck)
	{
		int ArticyDifficulty(DialogueSource source)
			=> source.Manager.mapping.DifficultyToArticy(ck.difficulty);

		fields.Set("Conversant", FieldType.Actor, source.Manager.mapping.SkillToActorID(ck.skill).ToString());
		fields.Set("FlagName", FieldType.Text, $"{source.Guid}.checks.{ck.id}");
		source.Add(new Variable($"checks.{ck.id}", false));

		fields.Set("SkillType", FieldType.Text, source.Manager.mapping.SkillToArticyId(ck.skill));
		// These two fields are required for all checks but do nothing AFAICT.
		fields.Set("Articy Id", FieldType.Text, "0x0000000000000000");
		fields.Set("check_target", FieldType.Text, "0x0000000000000000");

		if (ck.modifiers.Count > 10)
			source.Parent.LogWarning($"more than 10 modifiers found for a check in {parentConv} @ {fields.id}; some will be ignored");

		for (int i = 1; i <= 10; i++)
		{
			string value = "", variable = "", tooltip = "";
			if (ck.modifiers.Count > i)
			{
				var modifier = ck.modifiers[i];

				string varName = $"modifier.{parentConv.id}.{fields.id}.{modifier.id}";
				source.Add(new Variable(varName, FieldType.Boolean));
				variable = $"{source.Guid}.{varName}";

				value = modifier.delta.ToString();
				tooltip = modifier.tooltip;
			}
			fields.Set($"modifier{i}", FieldType.Number, value);
			// TODO: variable is not the name of the variable but a LUA conditions string!
			fields.Set($"variable{i}", FieldType.Text, variable);
			fields.Set($"tooltip{i}", FieldType.Text, tooltip);
		}

		if (ck.kind == ActiveCheck.Kind.White)
		{
			fields.Set("Forced", false);
			fields.Set("DifficultyWhite", ArticyDifficulty(source));

			// var wc = new WhiteCheck();
			// wc.Actor = source.manager.GetActor(pcEntry.ActorID);
			// wc.

			// FailedWhiteChecks.AddToCaches(wc);
		}
		else if (ck.kind == ActiveCheck.Kind.Red)
		{
			fields.Set("DifficultyRed", ArticyDifficulty(source));
		}
		else
		{
			fields.Set("DifficultyAtmo", ArticyDifficulty(source));
			fields.Set("AlwaysSucceed", FieldType.Number, (ck.kind == ActiveCheck.Kind.AlwaysSucceed).ToString());
		}
	}

	public void Cost(CostCheck ck)
	{
		fields.Set("ClickCost", ck.cost);
		fields.Set("HiddenNotEnough", ck.hideIfNotPayable);
		// i don't think this field actually does anything.
		fields.Set("CostOnce", true);
	}

	public void Passive(PassiveCheck ck)
	{
		int ArticyDifficulty(DialogueSource source) => source.Manager.mapping.DifficultyToArticy(ck.difficulty);

		fields.Set("DifficultyPass", ArticyDifficulty(source));
		if (ck.speakOnFailure) fields.Set("Antipassive", true);
		fields.Set("Actor", source.Manager.mapping.SkillToActorID(ck.skill));
	}
}

public class PixelsToDisco
{
	DialogueSource source;
	DiscoManager Manager => source.Parent.Manager;

	public PixelsToDisco(DialogueSource source)
	{
		this.source = source;
	}

	public string NameOf(PC.Asset asset)
	{
		if (asset.Name == null) return asset.id.ToString();

		string name = asset.Name;
		if (name.StartsWith(source.Guid + "."))
			name = name.Substring(source.Guid.Length + 1)!;
		return name;
	}

	public Variable Uncrush(PC.Variable variable) => new Variable(
		NameOf(variable),
		(FieldType)variable.LookupInitialValueType(),
		variable.InitialValue
	);

	public Actor Uncrush(PC.Actor actor) => new Actor(NameOf(actor), actor.Name);
	public Conversation Uncrush(PC.Conversation conversation)
	{
		var cv = new Conversation(NameOf(conversation), new List<Line>());
		int i = 0;
		foreach (var entry in conversation.dialogueEntries)
			cv.lines.Add(Uncrush(i, entry));

		return cv;
	}
	public Line Uncrush(int i, PC.DialogueEntry entry) => new Line(i, entry.currentDialogueText);
}
