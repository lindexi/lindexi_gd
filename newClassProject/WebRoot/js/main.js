$(document).ready(function() {
	
	$("textarea[name='content']").val("请输入需发布的作业内容");
	
	$("textarea[name='content']").click(function() {
		if($('textarea').val()=="请输入需发布的作业内容") {
			$('textarea').val("");	
			$('textarea').css("color","black");
		}
	});

	$("form[id='homework']").submit(function() {
		if($.trim($("textarea[name='content']").val()) == "请输入需发布的作业内容" || $.trim($("textarea[name='content']").val()) == "") {
			$('textarea').val("请输入需发布的作业内容");	
			$('textarea').css('color','red');	
			return false;
		}
		return true;
	});
});