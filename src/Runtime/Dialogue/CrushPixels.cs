using System.Collections.Generic;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime.Dialogue;

public class DiscoToPixels
{
	public PC.Link Crush(DiscoSource source, Link link)
	{
		var pcLink = new PC.Link();
		pcLink.isConnector = true;
		if (link.from != null)
		{
			pcLink.originConversationID = link.from.conversation.ResolveId(source.Manager);
			pcLink.originDialogueID = link.from.lineID;
		}
		pcLink.destinationConversationID = link.to.conversation.ResolveId(source.Manager);
		pcLink.destinationDialogueID = link.to.lineID;

		return pcLink;
	}
	public PC.DialogueEntry Crush(DiscoSource source, Line line, AssetRef parentConv)
	{
		var pcEntry = new PC.DialogueEntry();
		pcEntry.conversationID = parentConv.ResolveId(source.Manager);
		pcEntry.id = line.internalID;
		pcEntry.fields = new();
		pcEntry.conditionsString = line.condition ?? "";
		pcEntry.userScript = line.script ?? "";
		pcEntry.ActorID = line.speaker?.ResolveId(source.Manager) ?? 0;
		// NB: currentDialogueText is usually the same, *except* for the fact it won't create a new field when necessary...
		pcEntry.DialogueText = line.text;
		pcEntry.Title = line.title ?? line.text ?? $"{source.Guid}:{parentConv.id}#{line.internalID}";
		if (line.sequence != null) pcEntry.Sequence = line.sequence;
		if (line.sequence != null) pcEntry.ResponseMenuSequence = line.menuSequence;
		foreach (var link in line.links)
			pcEntry.outgoingLinks.Add(Crush(source, link));


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

	public PC.Conversation Crush(DiscoSource source, Conversation conv)
	{
		var pcConv = new PC.Conversation();
		pcConv.fields = new();
		pcConv.Name = conv.id;
		pcConv.Title = conv.id;
		pcConv.dialogueEntries = new();

		var handle = conv.Ref;
		foreach (var line in conv.lines)
		{
			pcConv.dialogueEntries.Add(Crush(source, line, handle));
		}


		return pcConv;
	}

	public static string? EncodeTextureName(string source, string? name) => name == null ? null : $"\0EXTRA\0{source}\0{name}";

	public PC.Actor Crush(DiscoSource source, Actor actor)
	{
		var pcActor = new PC.Actor();
		pcActor.fields = new();
		pcActor.Name = actor.displayName;
		pcActor.textureName = EncodeTextureName(source.Guid, actor.portraitName);
		pcActor.fields.Add(new PC.Field("short_description", "PLACEHOLDER DESCRIPTION", PC.FieldType.Text));
		pcActor.fields.Add(new PC.Field("LongDescription", "PLACEHOLDER LONG DESCRIPTION", PC.FieldType.Text));

		return pcActor;
	}

	public PC.Variable Crush(DiscoSource source, Variable var)
	{
		var pcVar = new PC.Variable();
		pcVar.fields = new();
		pcVar.Name = $"{source.Guid}.{var.id}";
		pcVar.InitialValue = var.initialValue;
		pcVar.Description = ""; // TODO
		pcVar.Type = (PC.FieldType)var.type;

		return pcVar;
	}

	public PC.Asset Crush(DiscoSource source, Asset asset) => asset.Type switch
	{
		AssetType.Actor => Crush(source, (Actor)asset),
		AssetType.Conversation => Crush(source, (Conversation)asset),
		AssetType.Variable => Crush(source, (Variable)asset),
		_ => throw new System.NotSupportedException("expected an actor, conversation or varaible"),
	};
}

public class EditFieldsForDialogue : DialogueNodeVisitor
{
	public FieldEditor fields;
	public DiscoSource source;
	public AssetRef parentConv;

	public EditFieldsForDialogue(FieldEditor fields, DiscoSource source, AssetRef parentConv)
	{
		this.fields = fields;
		this.source = source;
		this.parentConv = parentConv;
	}

	public void Active(ActiveCheck ck)
	{
		int ArticyDifficulty(DiscoSource source)
			=> source.Manager.Dialogue.mapping.DifficultyToArticy(ck.difficulty);

		fields.Set("Conversant", FieldType.Actor, source.Manager.Dialogue.mapping.SkillToActorID(ck.skill).ToString());
		fields.Set("FlagName", FieldType.Text, $"{source.Guid}.checks.{ck.id}");
		source.Assets.Add(new Variable($"checks.{ck.id}", false));

		fields.Set("SkillType", FieldType.Text, source.Manager.Dialogue.mapping.SkillToArticyId(ck.skill));
		// These two fields are required for all checks but do nothing AFAICT.
		fields.Set("Articy Id", FieldType.Text, "0x0000000000000000");
		fields.Set("check_target", FieldType.Text, "0x0000000000000000");

		if (ck.modifiers.Count > 10)
			source.Log.LogWarning($"more than 10 modifiers found for a check in {parentConv} @ {fields.id}; some will be ignored");

		for (int i = 1; i <= 10; i++)
		{
			string value = "", variable = "", tooltip = "";
			if (ck.modifiers.Count > i)
			{
				var modifier = ck.modifiers[i];

				string varName = $"modifier.{parentConv.id}.{fields.id}.{modifier.id}";
				source.Assets.Add(new Variable(varName, FieldType.Boolean));
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
		int ArticyDifficulty(DiscoSource source) => source.Manager.Dialogue.mapping.DifficultyToArticy(ck.difficulty);

		fields.Set("DifficultyPass", ArticyDifficulty(source));
		if (ck.speakOnFailure) fields.Set("Antipassive", true);
		fields.Set("Actor", source.Manager.Dialogue.mapping.SkillToActorID(ck.skill));
	}

	public void Empty(EmptyDialogueNode ck) { }
}

public class PixelsToDisco
{
	public string NameOf(PC.Asset asset)
	{
		if (asset.Name == null) return asset.id.ToString();
		else return asset.Name;
	}

	public Variable Uncrush(PC.Variable variable) => new Variable(
		NameOf(variable),
		(FieldType)variable.LookupInitialValueType(),
		variable.InitialValue
	);

	public static bool TryDecodeTextureName(string? textureName, out string source, out string path)
	{
		if (textureName == null || !textureName.StartsWith("\0EXTRA\0"))
		{
			source = "";
			path = "";
			return false;
		}

		textureName = textureName.Substring(7);
		int splitAt = textureName.IndexOf('\0');

		source = textureName.Substring(0, splitAt);
		path = textureName.Substring(splitAt + 1);
		return true;
	}

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

	public Asset Uncrush(PC.Asset ass)
	{
		bool TryCast<T>(out T casted) where T : class
		{
			casted = (ass as T)!;
			return ass is T;
		}

		if (TryCast<PC.Actor>(out var actor)) return Uncrush(actor);
		else if (TryCast<PC.Conversation>(out var conversation)) return Uncrush(conversation);
		else if (TryCast<PC.Variable>(out var variable)) return Uncrush(variable);
		else throw new System.NotSupportedException("expected an actor, conversation or varaible");
	}
}
