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
<title>完善个人信息</title>
<c:set value="${pageContext.request.contextPath}" var="path" scope="page"/>
<link rel="stylesheet"
	href="http://keleyi.com/keleyi/pmedia/jquery/ui/1.10.3/css/smoothness/jquery-ui.min.css" />

<%--<script type="text/javascript"	src="http://keleyi.com/keleyi/pmedia/jquery-1.9.1.min.js"></script>--%>
<script type="text/javascript" src="${path}/jquery/jquery-1.11.2.js"></script>
<script
	src="http://keleyi.com/keleyi/pmedia/jquery/ui/1.10.3/js/jquery-ui-1.10.3.min.js"
	type="text/javascript"></script>

<script type="text/javascript" src="${path}/js/stuInfo.js"></script>

<link rel="stylesheet" href="${path}/css/stuInfo.css">

<script type="text/javascript">
	function addFocusMessage() {
		$("#dialog").dialog({
			resizable : false,
			height : 240,
			width : 400,
			modal : true,
			buttons : {
				"添加" : function() {
					var addFocus = 
"<span>用户名:<input type='text' class='focusPerson' style='width: 25%' >&nbsp;<select class='status'><option value='1'>教师</option><option value='0' selected='selected'>学生</option></select>&nbsp;<select  class='focusSub'><option value=''>关注科目</option></select>&nbsp;<img class='Subtraction' alt='这是一幅图片' src='img/Subtraction.png' style='width: 7%;height: 15%;' onclick='removeSpan(this)'></span>";
					$("#dialog").append(addFocus);
				},
				"确定" : function() {
					//将对话框的信息转换为一个字符串focusPSInfo
					var focusPSInfo = "";
					var closeDialog = true;
					$("#dialog>span").each(function(){
						//接收关注信息
						 var focusPerson = $(this).children(".focusPerson").val();
						 var status = $(this).children(".status").val();
						 var focusSub = $(this).children(".focusSub").val();
						 //校验是否为空然后
						 if(""==focusPerson||""==focusSub||""==focusPerson.trim()||""==focusSub.trim()) {
							 alert("信息不能为空");
							 closeDialog=false;
							 return false;
						 }else {
						 focusPSInfo+="<"+focusPerson+","+status+","+focusSub+">"; 
							//alert(focusPSInfo);
							var previousMess = $("[name='focusPSInfo']").val();
							$("[name='focusPSInfo']").val(previousMess + focusPSInfo);
						 }
					});
					if(closeDialog){
					 $(this).dialog("close");
					}
				},
				"取消" : function() {
					$(this).dialog("close");
				}
			}
		});
	}

	function removeSpan(obj){
		$(obj).parents('span').remove();
	}
	
	function getSubject(){
		var focusPerson = $(".focusPerson").val();
		var status = $(".status").val();
	    $.getJSON("homeworkController.do?getOthersSubjects",
	    		{othersName:focusPerson,userStatus:status},
	    	function(homeworks){
	    			//if(homeworks.length<=0){return;}
	    			$(".focusSub").empty();
	    			var option = '<option value="">关注科目</option>';
	    			$(".focusSub").append(option);
	    			for(var i=0;i<homeworks.length;i++){
	    				if(homeworks[i]==""){continue;}
	    				var option = '<option value="'+homeworks[i]+'">'+homeworks[i]+'</option>';
	    				$(".focusSub").append(option);
	    			}
	    	});
	}

</script>
<style type="text/css">
#dialog {
	display: none;
}
</style>
</head>
<body>

	<h1 align="center">完善个人信息</h1>
	<div id="container">
		<div id="header"></div>
		<div>
			<form action="userController.do?stuInfo" method="post">
				<div id="email">
					<span>邮箱: </span><input type="text" name="email" value="${student.email }"/>
				</div>
				<div class="main" id="studentNumber">
					<span>学号 :</span><input type="text" name="stuNum" value="${student.stuNum }"/>
				</div>
				<div class="main" id="grade">
					<span>年级 :</span><input type="text" name="grade" value="${student.grade }"/>
				</div>
				<div class="main" id="myclass">
					<span>班级 :</span><input type="text" name="classStr" value="${student.classStr }"/>
				</div>
				<div class="main" id="realName">
					<span>真实姓名 :</span><input type="text" name="realName" value="${student.realName }"/>
				</div>
				<div class="main" id="phoneNum">
					<span>手机号码 :</span><input type="text" name="telephone" value="${student.telephone }"/>
				</div>
				<div class="main" id="qqNum">
					<span>qq号码 :</span><input type="text" name="qqNum" value="${student.qqNum }"/>
				</div>
				<div id="pubWorkSubject">
					<span>发布作业科目 </span><input type="text" name="pubWorkSubject" />
				</div>
				<div class="main">发布作业的科目含有多个时，请使用逗号分隔。</div>

				<div >
					<input type="button" onclick="addFocusMessage()" value="关注人信息" />
				</div>
				<input type="text" name="focusPSInfo" readonly="readonly">
				<div id="dialog" title="关注人信息">
					<span>
				         用户名:<input type="text" class="focusPerson" style="width: 25%" onblur = "getSubject()">
						<select class="status" onchange="getSubject()">
							<option value="1">教师</option>
							<option value="0" selected>学生</option>
						</select>
						<select class="focusSub">
							<option value="">关注科目</option>
						</select>
						<img class='Subtraction' class='Subtraction' alt='这是一幅图片' src='img/Subtraction.png' style='width: 7%;height: 15%;' onclick="removeSpan(this)">
					</span>
				</div>
				<div>
					<input type="submit" value="提交">
				</div> 
			</form>
		</div>

	</div>
</body>
</html>