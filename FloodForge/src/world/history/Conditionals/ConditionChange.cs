using FloodForge.World;

namespace FloodForge.History;

public class ConditionChange : Change {
	protected Conditional relevantConditional;
	protected List<Condition> conditions = [];
	protected List<int> indices = [];
	protected bool adding;

	public ConditionChange(Conditional conditional, bool adding) {
		this.relevantConditional = conditional;
		this.adding = adding;
	}

	public void AddCondition(Condition modifiedCondition) {
		this.conditions.Add(modifiedCondition);
	}

	protected void Add() {
		int i = 0;
		this.conditions.ForEach(c => { this.relevantConditional.conditions.Insert(this.indices.Count > i ? this.indices[i] : this.relevantConditional.conditions.Count, c); i++; });
	}

	protected void Remove() {
		if (this.indices.Count == 0)
			this.conditions.ForEach(c => this.indices.Add(this.relevantConditional.conditions.IndexOf(c)));
		this.conditions.ForEach(c => this.relevantConditional.conditions.Remove(c));
	}

	public override void Undo() {
		if (this.adding)
			this.Remove();
		else
			this.Add();
	}

	public override void Redo() {
		if (this.adding)
			this.Add();
		else
			this.Remove();
	}
}