##登录操作
提交post请求：
  url=url/tgrfswo/user/login
提交数据 
 - 用户名：username
 - 密码：password
返回json格式：
`{errorMsg: ,token: }`
 - 用户名密码正确时errorMsg为空，token为一个8位随机生成字符串（只生成一次）
 - 用户名不存在时errorMsg:"用户名不存在",token为空
 - 用户名存在但密码错误：errorMsg："用户名或密码错误"，token为空

 ##注册操作：
 提交post请求url：url/tgrfswo/user/register
 提交数据：
  - 用户名：username
  - 密码：password
返回json格式
`{errorMsg,token}`
  - 用户名已经存在时errorMsg:"用户名已存在"，token为空
  - 用户名格式不正确时：errorMsg:"用户名格式不正确",token为空
  - 密码格式不正确时：errorMsg:"密码格式不正确",token为空
  - 注册成功时：errorMsg为空，token为一个8位的随机生成字符串（只生成一次）

##注册成功后填写需要发布作业的课程：
提交（post请求）url：url/tgrfswo/homeWork/setCourse
提交数据：
 - token
 - publishCourseName
 发布作业的名称，多个课程时使用英文逗号隔开，但以一个字符串提交
返回json格式：{errorMsg: } 
 - 当token错误时：errorMsg:"请重新登录"
 - 当token正确时：errorMsg为空

##用户发布作业
界面post请求url:url/tgrfswo/homeWork/getCourse
提交数据：token
返回json格式：`{errorMsg:,course:[]}`
 - 当token错误时errorMsg:"请重新登录"，course为空
 - 当token正确时errorMsg为空,返回数据库中course的数组

提交post请求url：url/tgrfswo/homeWork/publish
提交数据：
 - token
 - content
 作业内容
 - course
 课程
 - submitTime作业提交时间
 格式：yyyy-MM-dd hh:mm:ss 作业发布时间系统默认，不需提交
返回json格式：{errorMsg:}
 - 当token错误，errorMsg:"请重新登录"
 - 当token正确，当课程为空或在该用户的课程中不存在，errorMsg:"课程名称错误"
 - 当token正确，当提交时间为空或是当前时间的过去时,errorMsg:"请选择正确时间"
 - 当token正确，当content为空，errorMsg:"作业内容不能为空"
 - (token正确但其他信息有误时错误信息会追加在一起，用逗号分开)
 - 当token正确且其他条件正确，errorMsg为空

##用户关注课程作业操作
 提交post请求url：url/tgrfswo/user/focus
 提交数据：token
 返回json格式：{errorMsg:,focusedUsername:[]}
 focusedUsername表示被关注的人，即数据库中的用户
 - 当token错误时errorMsg:"请重新登录"，focusedUsername为空
 - 当token正确时errorMsg为空,返回数据库中focusedUsername的数组

当用户选中一个被关注人时
提交url：url/tgrfswo/user/getCourse
提交数据：
 - token
 - focusedUsername
 被关注人姓名
返回json格式：{errorMsg:,course[]}
 - 当token错误时，errorMsg:"请重新登录"course为空
 - 当token正确时：errorMsg为空，course是被关注人的课程。

##作业界面获取作业时
提交url：url/tgrfswo/homeWork/get
提交的数据：token
 - Token正确时返回数据(一个json数组)：[{publisherName:,content:,publishTime:,submitTime:},]
 - Token错误时返回一个空json格式[{}].
