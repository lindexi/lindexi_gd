namespace MathGraphs.Neural;

public enum DatasetPreparationStage { Tokenizing, BuildingDataset, Grouping, SortingGroups, BuildingPartitions, Completed }

/// <summary>数据准备进度；字节数为已消费输入字节，样本数不包含空行。</summary>
public sealed record DatasetProgress(DatasetPreparationStage Stage, long Bytes, long? TotalBytes, long Samples, long Tokens);
