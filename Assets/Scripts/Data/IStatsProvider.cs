// 依据：程序基础知识库 5.3、5.9 第一层
public interface IStatsProvider
{
    float GetStat(StatType type);
    void ModifyStat(StatType type, float delta);
}
