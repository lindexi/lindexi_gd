namespace VolcEngineSdk;

public enum ArkTaskStatus
{
    /*
     *queued：排队中。
       running：任务运行中。
       cancelled：取消任务，取消状态24h自动删除（只支持排队中状态的任务被取消）。
       succeeded： 任务成功。
       failed：任务失败。
       expired：任务超时。

     */
    Queued,
    Running,
    Cancelled,
    Succeeded,
    Failed,
    Expired
}