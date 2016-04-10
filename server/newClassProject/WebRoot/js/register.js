function refresh() {
	var vcode = document.getElementById('randImage');
	vcode.src = "./getCheckCode.do?nocache=" + new Date().getTime();
}