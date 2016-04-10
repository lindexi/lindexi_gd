<%@ page language="java" import="java.util.*" pageEncoding="UTF-8"%>
<%@taglib uri="http://java.sun.com/jsp/jstl/fmt" prefix="fmt"%>
<%@ taglib uri="http://java.sun.com/jsp/jstl/core" prefix="c" %>
<!DOCTYPE html>
<html>
<head>
<c:set value="${pageContext.request.contextPath}" var="path" scope="page"/>
<title>作业页面</title>
<link rel="stylesheet" type="text/css" href="${path}/css/main.css">

<script type="text/javascript" src="${path}/jquery/jquery-1.11.2.js"></script>

<script type="text/javascript" src="${path}/js/main.js" ></script>

<script type="text/javascript">
$(document).ready(function(){
	
		$(".token").val(parseInt(Math.random()*1000));
		
	    $.getJSON("homeworkController.do?getHomework",function(homeworks){
	    	for(var i=0;i<homeworks.length;i++){
	    	var homework=homeworks[i];
	    	if(homework.subject!="班级公告"){//非班级公告
	    		//var aHomework = '<div><div align="center"><span>'+homework.subject+'</span></div><div><span>'+homework.content+'</span></div><br /><div id="time" align="right"><label>发布时间：</label><span>'+homework.publishTime+'</span>&nbsp;&nbsp;&nbsp;<br><label>提交时间：</label><span>'+homework.endTime+'&nbsp;</span><span>'+homework.specificTime+'</span></div><div align="right"><label>发布人：</label><span><span>'+homework.publishUsername+'</span></span>&nbsp;&nbsp;&nbsp;<p><form action="homeworkController.do?sendComment" method="post"><input type="text" name="comment" value="content" style="width:80%;"><input type="hidden" name="homeworkId" value="'+homework.id+'"><input type="submit" value="提交"></form></p><hr align="center" style="width:98%; color: #0000FF; border-style: dotted; border-width: 1"></div></div>';
	    		var aHomework = '<div><div align="center"><span>'+homework.subject+'</span></div><div><span>'+homework.content+'</span></div><br /><div id="time" align="right"><label>发布时间：</label><span>'+homework.publishTime+'</span>&nbsp;&nbsp;&nbsp;<br><label>提交时间：</label><span>'+homework.endTime+'&nbsp;</span><span>'+homework.specificTime+'</span></div><div align="right"><label>发布人：</label><span><span>'+homework.publishUsername+'</span></span>&nbsp;&nbsp;&nbsp;<p></p><hr align="center" style="width:98%; color: #0000FF; border-style: dotted; border-width: 1"></div></div>';
	    	}else{
	    		//var aHomework = '<div><div align="center"><span>'+homework.subject+'</span></div><div><span>'+homework.content+'</span></div><br /><div id="time" align="right"><label>发布时间：</label><span>'+homework.publishTime+'</span>&nbsp;&nbsp;&nbsp;<br></div><div align="right"><label>发布人：</label><span><span>'+homework.publishUsername+'</span></span>&nbsp;&nbsp;&nbsp;<p><form action="homeworkController.do?sendComment" method="post"><input type="text" name="comment" value="comment" style="width:80%;"><input type="hidden" name="homeworkId" value="'+homework.id+'"><input type="submit" value="提交"></form></p><hr align="center" style="width:98%; color: #0000FF; border-style: dotted; border-width: 1"></div></div>';
	    		var aHomework = '<div><div align="center"><span>'+homework.subject+'</span></div><div><span>'+homework.content+'</span></div><br /><div id="time" align="right"><label>发布时间：</label><span>'+homework.publishTime+'</span>&nbsp;&nbsp;&nbsp;<br></div><div align="right"><label>发布人：</label><span><span>'+homework.publishUsername+'</span></span>&nbsp;&nbsp;&nbsp;<p></p><hr align="center" style="width:98%; color: #0000FF; border-style: dotted; border-width: 1"></div></div>';
	    	}
	    	$(".main").append(aHomework);
	    	}
	    });
	    $.getJSON("homeworkController.do?getSubjects",function(subjects){
	    	for(var i=0;i<subjects.length;i++) {
	    		var option = '<option value="'+subjects[i]+'">'+subjects[i]+'</option>';
	    		$(".subject").append(option);
	    	}
	    	var option = '<option value="班级公告">班级公告</option>';
	    	$(".subject").append(option);
	    });
	});
</script>
</head>
<body>
	<h1 align="center">作业页面</h1>
	<div id="container">
		<div id="header">
			<form action="homeworkController.do?postHomework" method="post" id="homework">
				<input type="hidden" name="token" class="token">
				<select name="subject" class="subject">
					<option value="" selected="selected">请选择科目</option>
				</select>
				<span><a href="userController.do?update">更改个人详细信息</a></span>&nbsp;&nbsp;&nbsp;
				<span id="more"><a href="http://10.21.71.130:8080/login.html?lang=schinese">下载资源</a></span>
				<textarea name="content" ></textarea>
				<div align="right">
					提交作业时间：<input type="date" name="endTime" /> 
					具体时间：<select name="specificTime">
						<option value="" selected="selected">*请选择*</option>
						<option value="第一二节">第一二节 </option>
						<option value="第三四节">第三四节 </option>
						<option value="第六七节">第六七节 </option>
						<option value="第八九节">第八九节 </option>
						<option value="第十、十一、十二节">第十、十一、十二节 </option>
					</select>
					<input type="submit" value="发布">
				</div>
			</form>
		</div>
		
		<div class="main"></div>
	</div>
</body>
</html>
