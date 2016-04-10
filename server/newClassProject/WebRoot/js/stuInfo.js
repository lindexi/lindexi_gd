$(document).ready(function(){
	$("#focusPeople [name='status']").change(function() {
		var xmlhttp;
		if (window.XMLHttpRequest) {
			xmlhttp = new XMLHttpRequest();
		} else {
			xmlhttp = new ActiveXObject("Microsoft.XMLHTTP");
		}
		xmlhttp.onreadystatechange = function() {
			if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
//				document.getElementById("myDiv").innerHTML = xmlhttp.responseText;
				alert(1);
			}
		};
		xmlhttp.open("GET", "/ajax/demo_get.asp", true);
		xmlhttp.send();
	});
});

/*<span>关注人 :</span><input type="text" name="focusPeople" /> 
					<span>身份:</span>
					<select>
						<option value="" selected="selected">请选择</option>
						<option value="">身份</option>
						<option value="teacher">教师</option>
						<option value="student">学生</option>
					</select>
					<select name="focusSubject">
						<option>--科目--</option>
					</select>
					<img id="add" style="width:3%;height: 3%;" src="img/add.png"></img>*/

$(function(){
	  $("#form").submit(function(){
	   var paramList = [];
	    $( "#product_params div[class='param']" ).each(function(){
	         var param=$.trim($(this).children("span").text())+"_"+$(this).children("input").val()+$(this).children("input").attr("class");
	         paramList.push(param);
	    });
	    alert(paramList);
	    $("#param_list").val(paramList);
	   });
	    
	  var api = "",
	  count=1;
	  $( ".choose-dialog" ).click(function(){
	    
	   api = $.dialog({
	      id: 'testID2',
	      lock: true,
	      content : $( "#chooseMain" ).html(),
	      fixed: true,
	      title:"添加产品参数",
	      width:320,
	      height:240,
	      max: false,
	      min: false,
	      ok: function() {
	        var paramName=$(".chooseMain input[name='param_name']").val();
	        var paramValue=$(".chooseMain input[name='param_value']").val();
	        var paramClassify=$(".param_classify").val();
	        if($.trim(paramName)==""){
	         alert("参数名称不能为空!");
	         return false;
	        };
	        if($.trim(paramValue)==""){
	         alert("参数值称不能为空!");
	         return false;
	        };
	        if(paramClassify=="0"){
	         alert("请选择参数类型!");
	         return false;
	        }
	        var params="<div class='param' style='height: 30px'><span>"+paramName+" </span><input type='text' class='"+paramClassify+"' value='"+paramValue+"'/></div>";
	        $("#params").append(params);
	        count++;     
	                      },
	                     okVal: "确定",
	      cancelVal: '关闭',
	      cancel: true
	     });
	  });
	 });
