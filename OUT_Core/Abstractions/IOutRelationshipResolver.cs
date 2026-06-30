public interface IOutRelationshipResolver
{
    OUT_RelationshipKind Resolve(IOutFactionMember self, IOutFactionMember target);
}