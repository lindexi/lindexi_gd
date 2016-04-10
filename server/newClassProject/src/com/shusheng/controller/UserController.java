package com.shusheng.controller;

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.servlet.ModelAndView;

import com.shusheng.model.FocusMsg;
import com.shusheng.model.Student;
import com.shusheng.model.Subject;
import com.shusheng.model.Teacher;
import com.shusheng.model.User;
import com.shusheng.service.HomeworkService;
import com.shusheng.service.UserService;

/**
 * 用户controller,包含登录和注册
 * @author Administrator
 *
 */
@Controller
@RequestMapping("userController")
public class UserController {
	
	@Autowired
	private UserService userService;
	@Autowired
	private HomeworkService homeworkService;

	/**
	 * 前往登录页面
	 * @param request
	 * @param response
	 * @return
	 */
	@RequestMapping(method=RequestMethod.GET,params="login")
	public ModelAndView login(HttpServletRequest request,HttpServletResponse response) {
		return new ModelAndView("login");
	}
	
	/**
	 * 登录表单提交
	 * @param request
	 * @param response
	 * @return
	 */
	@RequestMapping(method=RequestMethod.POST,params="login")
	public ModelAndView doLogin(HttpServletRequest request,HttpServletResponse response){
		StringBuffer errorMsg = new StringBuffer();
		ModelAndView modelAndView = new ModelAndView();
		String username = request.getParameter("username");
		String password = request.getParameter("password");
		String userStatus = request.getParameter("userStatus");
		if (username==null||username.trim().length()<2) //用户名格式不正确
			errorMsg.append("用户名长度不正确,长度应该大于等于2小于等于6");
		if (password.trim().length()<4) 
			errorMsg.append("密码长度不正确,长度应该大于等于4小于等于20");
		if (errorMsg.length()!=0) {
			modelAndView.setViewName("login");
			modelAndView.addObject("username",username);
			modelAndView.addObject("password",password);
			modelAndView.addObject("errorMsg",errorMsg);
			return modelAndView;
		}
		int status = Integer.parseInt(userStatus);
		//检查表单数据，返回0正确，返回1用户名不存在，返回2密码错误
		int result = userService.doLogin(username,password,status);
		if(result==0) {//用户名密码正确
			Object object = userService.getUser(username,password,status);
			request.getSession().setAttribute("user",object);
			modelAndView.setViewName("main");
			return modelAndView;
			}
		if(result==1)//用户名不存在错误
			errorMsg.append("用户名不存在");
		if(result==2) //密码错误
			errorMsg.append("密码错误");
		
		modelAndView.setViewName("login");
		modelAndView.addObject("username",username);
		modelAndView.addObject("password",password);
		modelAndView.addObject("errorMsg",errorMsg);
		return modelAndView;
	}
	
	/**
	 * 前往注册
	 * @param request
	 * @param response
	 * @return
	 */
	@RequestMapping(method=RequestMethod.GET,params="register")
	public ModelAndView register(HttpServletRequest request,HttpServletResponse response) {
		return new ModelAndView("register");
	}
	
	@RequestMapping(method=RequestMethod.GET,params="update")
	public ModelAndView update(HttpServletRequest request,HttpServletResponse response){
		User user = (User) request.getSession().getAttribute("user");
		ModelAndView modelAndView = new ModelAndView();
		if(user.getUserStatus()==0){
			modelAndView.addObject("student",user).setViewName("stuInfo");
		}else{
			modelAndView.addObject("teacher",user).setViewName("teaInfo");
		}
		return modelAndView;
	}
	
	/**
	 * 注册表单提交
	 * @param request
	 * @param response
	 * @return
	 */
	@RequestMapping(method=RequestMethod.POST,params="register")
	public ModelAndView doRegister(HttpServletRequest request,HttpServletResponse response) {
		ModelAndView modelAndView = new ModelAndView();
		String username = request.getParameter("username");//用户名
		String password = request.getParameter("password");//密码
		String password2 = request.getParameter("password2");
		StringBuffer errorMsg = new StringBuffer();
		String userStatusParam = request.getParameter("userStatus");//用户身份,1表示教师，0表示学生
		if (username.trim().length()<2) 
			errorMsg.append("用户名长度应该大于等于2小于等于6,");
		if (password.trim().length()<4) 
			errorMsg.append("密码长度应该大于登录4且小于等于20,");
		if ("1".equals(userStatusParam)||"0".equals(userStatusParam)) {
		}else 
			errorMsg.append("用户身份不正确,");
		if (!password.equals(password2)) 
			errorMsg.append("密码与重复密码不一致");
		if (errorMsg.length()!=0) {
			modelAndView.setViewName("register");
			modelAndView.addObject("username",username);
			modelAndView.addObject("password",password);
			modelAndView.addObject("errorMsg",errorMsg);
			return modelAndView;
		}
		int userStatus = Integer.valueOf(userStatusParam);//注册用户状态
		boolean success = userService.doRegister(username,password,userStatus);//检查用户名密码格式，是否存在
		if(success&&userStatus==0) //学生成功注册
			modelAndView.setViewName("stuInfo");//前往完善学生信息
		if(success&&userStatus==1)//教师成功注册
			modelAndView.setViewName("teaInfo");
		if (success) {//注册成功，将用户对象放入session
			Object object = userService.getUser(username, password2, userStatus);
			request.getSession().setAttribute("user", object);
		}else{//注册失败，用户名已经存在
			modelAndView.setViewName("register");
			modelAndView.addObject("errorMsg","用户名已存在");
		}
		return modelAndView;
	}

	/**
	 * 前往填写学生详细信息页面
	 * @param request
	 * @param response
	 * @return
	 */
	@RequestMapping(method=RequestMethod.GET,params="stuInfo")
	public ModelAndView stuPersonalMsg(HttpServletRequest request,HttpServletResponse response){
		return new ModelAndView("stuInfo");
	}

	/**
	 * 学生个人信息提交
	 * @param request
	 * @param response
	 * @return
	 */
	@RequestMapping(method=RequestMethod.POST,params="stuInfo") 
	public ModelAndView doStuPersonalMsg(HttpServletRequest request,HttpServletResponse response){
		Student student = (Student) request.getSession().getAttribute("user");
		String email = request.getParameter("email");
		String stuNum = request.getParameter("stuNum");
		String grade = request.getParameter("grade");
		String classStr = request.getParameter("classStr");
		String realName = request.getParameter("realName");
		String telephone = request.getParameter("telephone");
		String qqNum = request.getParameter("qqNum");
		String pubWorkSubjectString = request.getParameter("pubWorkSubject");
		String focusPSInfo = request.getParameter("focusPSInfo");
		
		String[] pubWorkSubject  = pubWorkSubjectString.split(",");
		List<Subject> subjectList = new ArrayList<Subject>();
		for (int i = 0; i < pubWorkSubject.length; i++) {
			String subjectName = pubWorkSubject[i];
			Subject subject = new Subject();
			subject.setName(subjectName);
			subject.setUserId(student.getId());
			subjectList.add(subject);
		}
		
		student.setClassStr(classStr);
		student.setEmail(email);
		List<FocusMsg> focusMsgs = getFocusMsg(student,focusPSInfo);
		student.setGrade(grade);
		student.setQqNum(qqNum);
		student.setRealName(realName);
		student.setStuNum(stuNum);
		student.setTelephone(telephone);
		userService.update(student);//更新学生
		homeworkService.saveSubjects(subjectList);//更新发布作业的课程
		userService.saveFocusMsg(focusMsgs);//更新关注的课程
		return new ModelAndView("main");
	}
	
	/**
	 *前往填写教师详细信息
	 * @param request
	 * @param response
	 * @return
	 */
	@RequestMapping(method=RequestMethod.GET,params="teaInfo")
	public ModelAndView teaPersonalMsg(HttpServletRequest request,HttpServletResponse response){
		return new ModelAndView("teaInfo");
	}
	
	/**
	 * 提交教师个人详细信息
	 * @param request
	 * @param response
	 * @return
	 */
	@RequestMapping(method=RequestMethod.POST,params="teaInfo")
	public ModelAndView doTeaPersonalMsg(HttpServletRequest request,HttpServletResponse response){
		Teacher teacher = (Teacher) request.getSession().getAttribute("user");
		String email = request.getParameter("email");
		String teaNum = request.getParameter("teaNum");
		String realName = request.getParameter("realName");
		String telephone = request.getParameter("phoneNum");
		String qqNum = request.getParameter("qqNum");
		
		String[] pubWorkSubject = request.getParameter("pubWorkSubject").split(",");
		List<Subject> subjectList = new ArrayList<Subject>();
		for (int i = 0; i < pubWorkSubject.length; i++) {
			String subjectName = pubWorkSubject[i];
			Subject subject = new Subject();
			subject.setUserId(teacher.getId());
			subject.setName(subjectName);
			subjectList.add(subject);
		}
		teacher.setEmail(email);
		teacher.setQqNum(qqNum);
		teacher.setRealName(realName);
		teacher.setTeaNum(teaNum);
		teacher.setTelephone(telephone);
		userService.update(teacher);
		homeworkService.saveSubjects(subjectList);
		return new ModelAndView("main");
	}
	
	/**
	 * 通过页面的字符串返回切割后的关注信息list
	 * @param focusPSInfoList
	 * @return
	 */
	private List<FocusMsg> getFocusMsg(User user,String focusPSInfoList) {
		if (focusPSInfoList==null) {
			return null;
		}
		List<FocusMsg> focusMsgs = new ArrayList<FocusMsg>();
			//使用正则表达式对关注人信息进行分割<,,>,<,,>
			Pattern p = Pattern.compile("<[\u4e00-\u9fa5]*,\\w*,[\u4e00-\u9fa5]*>");
			Matcher matcher = p.matcher(focusPSInfoList);
			while(matcher.find()) {//将符合格式的字符串添加到list中
				String temp = matcher.group();
				String tempMsg = temp.substring(1,temp.length()-1);
				String[] tempStrings = tempMsg.split(",");
				String focusedName = tempStrings[0];//拿到关注人姓名
				String focusPSStatus = tempStrings[1];//拿到关注人身份
				String focusSubject = tempStrings[2];//拿到关注科目名称
				//封装成为一个学生学委对象
				FocusMsg focusMsg = new FocusMsg();
				focusMsg.setUsername(user.getUsername());
				focusMsg.setUserStatus(user.getUserStatus());
				focusMsg.setFocusedName(focusedName);
				focusMsg.setSubjectName(focusSubject);
				focusMsg.setFocusPStatus(Integer.parseInt(focusPSStatus));
				//添加到容器
				focusMsgs.add(focusMsg);
			}
//		}
		return focusMsgs;
	}

}
