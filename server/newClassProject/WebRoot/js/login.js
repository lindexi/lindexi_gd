function refresh() {
	var vcode = document.getElementById('randImage');
	vcode.src = "./getCheckCode.do?nocache=" + new Date().getTime();
}

$(document).ready(function() {

	$('input').click(function() {
		if ($(this).val() == "提交") {
			// 提交按钮时都什么不做
		} else {
			if ($.trim($(this).val()) == "未输入" || $.trim($(this).val()) == "") {
				$(this).val("");
				$(this).css("background", "white");
			}
		}
	});
});


$(document).ready(function() {
	$('form').submit(function() {
		var flag = true;
		// 去掉前后空格
		if ($.trim($("input[name='username']").val()) == "" || $("input[name='username']").val()=="未输入") {
			$("input[name='username']").css("background", "yellow");
			$("input[name='username']").val("未输入");
			flag = false;
		}
		// 去掉前后空格
		if ($.trim($("input[name='password']").val()) == "") {
			$("input[name='password']").val("");
			$("input[name='password']").css("background", "yellow");
			flag = false;
		}

		// 去掉前后空格
		if ($.trim($("input[name='checkCode']").val()) == "" || $("input[name='checkCode']").val()=="未输入") {
			$("input[name='checkCode']").css("background", "yellow");
			$("input[name='checkCode']").val("未输入");
			flag = false;
		}
		return flag;
	});
});
