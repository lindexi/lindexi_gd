<%@ page language="java" import="java.util.*" pageEncoding="UTF-8"%>
<%
String path = request.getContextPath();
String basePath = request.getScheme()+"://"+request.getServerName()+":"+request.getServerPort()+path+"/";
%>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN">
<html>
  <head>
    <base href="<%=basePath%>">
    
    <title></title>
    
	<meta http-equiv="pragma" content="no-cache">
	<meta http-equiv="cache-control" content="no-cache">
	<meta http-equiv="expires" content="0">    
	<meta http-equiv="keywords" content="keyword1,keyword2,keyword3">
	<meta http-equiv="description" content="This is my page">

	<link rel="stylesheet" type="text/css" href="${path}/css/teaInfo.css">

  </head>
  <script type="text/javascript" src="${path}/jquery/jquery-1.9.1.min.js"></script>
  <body>
  <h1 align="center">完善个人信息</h1>
  <div id="container" align="center">
  	<div id="header"></div>
  	<div>
	  	<form action="userController.do?teaInfo" method="post">
			<div id="email"><span>邮箱: </span><input type="text" name="email" /></div>
	  		<div class="main" id="realName"><span>教工号:</span><input type="text" name="teaNum" /></div>
	  		<div class="main" id="realName"><span>真实姓名  :</span><input type="text" name="realName" /></div>
	  		<div class="main" id="phoneNum"><span>手机号码  :</span><input type="text" name="phoneNum" /></div>
	  		<div class="main" id="qqNum"><span>qq号码  :</span><input type="text" name="qqNum" /></div>
	  			<div class="main" id="pubWorkSubject"><span>发布作业科目 </span><input type="text" name="pubWorkSubject" /></div>
	  		<div class="main" >发布作业的科目含有多个时，请使用逗号分隔。</div>
	  		<div align="center"><input type="submit" value="提交"/></div>
	  	</form>
  	</div>
  </div>
  </body>
</html>
