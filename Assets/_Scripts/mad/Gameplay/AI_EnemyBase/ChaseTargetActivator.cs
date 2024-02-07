namespace WGRF.AI
{
    public class ChaseTargetActivator : INode
    {
        EnemyNodeData nodeData;

        ChaseAttackSelector chaseTarget;

        public ChaseTargetActivator(EnemyNodeData nodeData, ChaseAttackSelector chaseTarget)
        {
            this.nodeData = nodeData;
            this.chaseTarget = chaseTarget;
        }

        public INodeData GetNodeData()
        {
            return nodeData;
        }

        /// <summary>
        /// Call to check if the nodeData target has been marked as found.
        /// <para>If yes, Run and return the ChaseAttackSelector return value. Else return false.</para>
        /// <para>If the agent nodeData CanShoot value is false the node returns true.</para>
        /// </summary>
        /// <returns></returns>
        public bool Run()
        {
            //This forces the branch to remain active but frozen.
            if (!nodeData.CanShoot) return true;

            return chaseTarget.Run();
        }
    }
}