<%@ page language="java" import="java.util.*" pageEncoding="UTF-8"%>
<%@ taglib uri="http://java.sun.com/jsp/jstl/core" prefix="c" %>
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

<title>注册页面</title>

<meta http-equiv="pragma" content="no-cache">
<meta http-equiv="cache-control" content="no-cache">
<meta http-equiv="expires" content="0">
<meta http-equiv="keywords" content="keyword1,keyword2,keyword3">
<meta http-equiv="description" content="This is my page">

<c:set value="${pageContext.request.contextPath}" var="path" scope="page"/>

<link rel="stylesheet" type="text/css" href="${path}/css/register.css">

<script type="text/javascript" src="${path}/js/register.js" ></script>

<script type="text/javascript" src="${path}/jquery/jquery-1.9.1.min.js"></script>

</head>

<body>
	<h1 align="center">注册页面</h1>
	<div id="container">
		<div id="head" align="center">欢迎您注册网站，如果您已拥有账户，则可以点击此登陆</div>
		<div class="main">
			<form action="userController.do?register" method="post">
				
				<div id="username"><span>用户名:</span><input type="text" name="username" /></div>
				
				<div class="password"><span>密码: </span><input type="password" name="password" /></div>
				
				<div class="password"><span>确认密码:</span><input type="password" name="password2" /></div>
				
				<div ><span>身份：</span>教师<input type="radio" name="userStatus" value="1"/>&nbsp;&nbsp;&nbsp;学生<input type="radio" name="userStatus" value="0" checked="checked"/></div>
				
				<div align="center"><input type="submit" value="提交"/></div>
				<p style="background-color: red">${errorMsg }</p>
				<div align="right"><a href="userController.do?login">前往登录页面</a></div>
			</form>
		</div>
	</div>
</body>
</html>
