<%@ page language="java" import="java.util.*" pageEncoding="UTF-8"%>
<%
	String path = request.getContextPath();
	String basePath = request.getScheme() + "://"
			+ request.getServerName() + ":" + request.getServerPort()
			+ path + "/";
%>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN">
<html>
<head>
<base href="<%=basePath%>">

<title>登录页</title>

<meta http-equiv="pragma" content="no-cache">
<meta http-equiv="cache-control" content="no-cache">
<meta http-equiv="expires" content="0">
<meta http-equiv="keywords" content="keyword1,keyword2,keyword3">
<meta http-equiv="description" content="This is my page">
<!--
	<link rel="stylesheet" type="text/css" href="styles.css">
	-->
</head>
<body>
	<div>
		<form action="userController.do?login" method="post">
			用户名：<input type="text" name="username" value="${username }" align="middle"><br> 
			密码：<input type="password" name="password" value="${password }" align="middle"><br>
			<span>身份：</span>
			教师<input type="radio" name="userStatus" value="1" />&nbsp;&nbsp;&nbsp;
			学生<input type="radio" name="userStatus" value="0" checked="checked" /> <input type="submit" value="提交">
		</form>
	</div>
	<span>${errorMsg }</span>
	<a href="userController.do?register">注册</a>
	<br>
</body>
</html>
