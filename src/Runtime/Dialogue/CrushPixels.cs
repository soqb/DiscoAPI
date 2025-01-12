using System;
using System.Collections.Generic;
using DiscoAPI.Common.Assets;
using DiscoAPI.Common.Dialogue;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime.Dialogue;

public class DiscoToPixels
{
	public PC.Link Crush(DiscoSource source, Link link) => Crush(source, link, null, 0);
	public PC.Link Crush(DiscoSource source, Link link, AssetLocation<Conversation>? parentConv, int convoId)
	{
		var pcLink = new PC.Link();
		pcLink.isConnector = true;
		if (link.from != null)
		{
			pcLink.originConversationID = link.from.conversation.Location == parentConv
				? convoId : link.from.conversation.ResolveCrushed(source.Manager)!.id;
			pcLink.originDialogueID = link.from.lineId;
		}
		pcLink.destinationConversationID = link.to.conversation.Location == parentConv
			? convoId : link.to.conversation.ResolveCrushed(source.Manager)!.id;
		pcLink.destinationDialogueID = link.to.lineId;

		return pcLink;
	}
	public PC.DialogueEntry Crush(DiscoSource source, Line line, AssetLocation<Conversation> parentConv, int convoId, int id)
	{
		var pcEntry = new PC.DialogueEntry();
		pcEntry.conversationID = convoId;
		pcEntry.id = id;
		pcEntry.fields = new();
		pcEntry.conditionsString = line.condition ?? "";
		pcEntry.userScript = line.script ?? "";
		pcEntry.ActorID = line.speaker == null ? 0 : line.speaker!.ResolveCrushed(source.Manager)!.id;
		// NB: currentDialogueText is usually the same, *except* for the fact it won't create a new field when necessary...
		pcEntry.DialogueText = line.text;
		pcEntry.Title = line.title ?? line.text ?? $"{source.Guid}:{parentConv.id}#{id}";
		if (line.sequence != null) pcEntry.Sequence = line.sequence;
		if (line.sequence != null) pcEntry.ResponseMenuSequence = line.menuSequence;
		foreach (var link in line.links)
			pcEntry.outgoingLinks.Add(Crush(source, new Link(new(parentConv, id), link), parentConv, convoId));


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

	public PC.Conversation Crush(DiscoSource source, Conversation conv, int convoId)
	{
		var pcConv = new PC.Conversation();
		pcConv.fields = new();
		pcConv.Name = conv.id;
		pcConv.Title = conv.id;
		pcConv.dialogueEntries = new();

		for (int i = 0; i < conv.lines.Count; i++)
			pcConv.dialogueEntries.Add(Crush(source, conv.lines[i], conv.Location, convoId, i));


		return pcConv;
	}

	public static string? EncodeTextureName(string source, string? name) => name == null ? null : $"\0EXTRA\0{source}\0{name}";
	public static string BuildArticyId(Asset asset)
	{
		Type t = asset.GetType();
		string assetKind = t.IsAssignableTo(typeof(Actor)) ? "actors"
			: t.IsAssignableTo(typeof(Conversation)) ? "conversations"
			: t.IsAssignableTo(typeof(Variable)) ? "variables"
			: throw new NotSupportedException();

		return $"{asset.source}:{assetKind}.{asset.id}";
	}

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
		pcVar.InitialValue = var.initialValue.ToString();
		pcVar.Description = ""; // TODO
		pcVar.Type = (PC.FieldType)var.Type;

		return pcVar;
	}

	public PC.Asset Crush(DiscoSource source, Asset asset, int convoId)
	{
		if (asset is Actor actor) return Crush(source, actor);
		else if (asset is Conversation conversation) return Crush(source, conversation, convoId);
		else if (asset is Variable variable) return Crush(source, variable);
		else throw new System.NotSupportedException("expected an actor, conversation, or varaible");
	}
}

public class EditFieldsForDialogue : IDialogueNodeVisitor
{
	public FieldEditor fields;
	public DiscoSource source;
	public IAssetRef<Conversation> parentConv;

	public EditFieldsForDialogue(FieldEditor fields, DiscoSource source, IAssetRef<Conversation> parentConv)
	{
		this.fields = fields;
		this.source = source;
		this.parentConv = parentConv;
	}

	public void Active(ActiveCheck ck)
	{
		int ArticyDifficulty(DiscoSource source)
			=> source.Manager.Dialogue.mapping.DifficultyToArticy(ck.difficulty);

		fields.Set("Conversant", FieldType.Actor, SkillUtils.SkillToPCActor(ck.skill).id.ToString());
		fields.Set("FlagName", FieldType.Text, $"{source.Guid}.checks.{ck.id}");
		source.Add(new Variable($"checks.{ck.id}", false));

		fields.Set("SkillType", FieldType.Text, SkillUtils.SkillToPCActor(ck.skill).LookupValue(ArticyBridge.ARTICY_ID_FIELD));
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

				string varName = $"modifier.{parentConv.Location.id}.{fields.id}.{modifier.id}";
				source.Add(new Variable(varName, false));
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
		fields.Set("CostOnce", !ck.repeatable);
	}

	public void Passive(PassiveCheck ck)
	{
		int ArticyDifficulty(DiscoSource source) => source.Manager.Dialogue.mapping.DifficultyToArticy(ck.difficulty);

		fields.Set("DifficultyPass", ArticyDifficulty(source));
		if (ck.speakOnFailure) fields.Set("Antipassive", true);
		fields.Set("Actor", SkillUtils.SkillToPCActor(ck.skill).id);
	}

	public void Empty(EmptyDialogueNode ck) { }
}

public class PixelsToDisco
{
	public static string NameOf(PC.Asset asset, string? name)
	{
		string id;
		if (name == null) id = asset.id.ToString();
		else id = name;

		return FormatUtils.Slugify(id);
	}


	public Variable Uncrush(PC.Variable variable)
	{
		FieldValue val = variable.Type switch
		{
			PC.FieldType.Boolean => variable.InitialBoolValue,
			PC.FieldType.Text => variable.InitialValue,
			PC.FieldType.Number => variable.InitialFloatValue,
			_ => throw new NotSupportedException(),
		};

		return new Variable(
			NameOf(variable, variable.Name),
			val
		);
	}

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

	public Actor Uncrush(PC.Actor actor)
	{
		var at = new Actor(NameOf(actor, actor.Name), actor.Name);
		if (!string.IsNullOrWhiteSpace(actor.Name)) at.displayName = actor.Name;

		return at;
	}

	public Conversation Uncrush(PC.Conversation conversation)
	{
		var cv = new Conversation(NameOf(conversation, conversation.Title), new List<Line>());
		if (!string.IsNullOrWhiteSpace(conversation.Title)) cv.title = conversation.Title;
		if (!string.IsNullOrWhiteSpace(conversation.Description)) cv.description = conversation.Description;

		// for (int i = 0; i < conversation.dialogueEntries.Count; i++)
		// 	cv.lines.Add(Uncrush(i, conversation.dialogueEntries[i]));

		return cv;
	}
	public Line Uncrush(PC.DialogueEntry entry) => new Line(entry.currentDialogueText);

	public Asset Uncrush(PC.Asset ass)
	{
		Asset asset;

		if (ass is PC.Actor actor) asset = Uncrush(actor);
		else if (ass is PC.Conversation conversation) asset = Uncrush(conversation);
		else if (ass is PC.Variable variable) asset = Uncrush(variable);
		else throw new System.NotSupportedException("expected an actor, conversation, or varaible");

		// for now bro...
		asset.source = "disco";

		return asset;
	}
}
