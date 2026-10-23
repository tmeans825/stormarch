using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class SiblingRuleTile : RuleTile
{
    public enum SiblingGroup
    {
        CliffTop,
        Water,
        Grass,
        Fence,
        Garden,
        Placeholder
    }
    public SiblingGroup siblingGroup;

    public bool hasRelative = false;
    public SiblingGroup relativeGroup;

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        if (other is RuleOverrideTile)
            other = (other as RuleOverrideTile).m_InstanceTile;

        switch (neighbor)
        {
            case TilingRule.Neighbor.This:
                {
                    return (other is SiblingRuleTile
                        && isSiblingOrRelated(other));
                }
            case TilingRule.Neighbor.NotThis:
                {
                    return !(other is SiblingRuleTile
                        && isSiblingOrRelated(other));
                }
        }

        return base.RuleMatch(neighbor, other);
    }

    private bool isSiblingOrRelated(TileBase other)
    {
        if (other is RuleOverrideTile)
            other = (other as RuleOverrideTile).m_InstanceTile;

        return ((other as SiblingRuleTile).siblingGroup == this.siblingGroup) || (this.hasRelative && ((other as SiblingRuleTile).siblingGroup == this.relativeGroup));
    }
}
