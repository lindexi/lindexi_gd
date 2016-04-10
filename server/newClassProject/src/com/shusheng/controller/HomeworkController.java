package com.shusheng.controller;

import java.io.UnsupportedEncodingException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.List;

import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.servlet.ModelAndView;

import com.shusheng.model.CommentEntity;
import com.shusheng.model.Homework;
import com.shusheng.model.Student;
import com.shusheng.model.User;
import com.shusheng.service.HomeworkService;
import com.shusheng.util.CommonUtil;

/**
 * 作业controller
 * @author Administrator
 *
 */
@Controller
@RequestMapping("/homeworkController")
public class HomeworkController {

	@Autowired
	private HomeworkService homeworkService;
	
	/**
	 * 作业发布的方法
	 * @return
	 */
	@RequestMapping(method=RequestMethod.POST,params="postHomework")
	public ModelAndView doHomework(HttpServletRequest request,HttpServletResponse response){
		User user = (User) request.getSession().getAttribute("user");
		String token = request.getParameter("token");
		String serverToken = (String) request.getSession().getAttribute("token");
		if (token!=null && token.equals(serverToken)) {//相同说明重复提交
			return new ModelAndView("main");
		}else {//token不相同说明第一次提交,就将该客户端的token保存
			request.getSession().setAttribute("token",token);
		}
		
		token = (String) request.getSession().getAttribute("token");
		String subject = request.getParameter("subject");
		String content = request.getParameter("content");
		String endTime = request.getParameter("endTime");
		String specificTime = request.getParameter("specificTime");
		Homework homework = new Homework();
		homework.setPublishTime(new SimpleDateFormat("yyyy-MM-dd").format(new Date()));
		homework.setSubject(subject);
		homework.setContent(content);
		homework.setEndTime(endTime);
		homework.setPublishUsername(user.getUsername());
		homework.setUserStatus(user.getUserStatus());
		homework.setSpecificTime(specificTime);
		if (user.getUserStatus()==0) {//学生
			Student student = (Student) user;
			homework.setClassStr(student.getClassStr());
		}
		boolean result = homeworkService.addHomework(homework);
		String errorMsg = "";
		if (!result) {//添加不成功
			errorMsg = "添加失败";
		}
		return new ModelAndView("main").addObject("errorMsg",errorMsg);
	}
	
	/**
	 * 页面获取作业
	 * @param request
	 * @param response
	 */
	@RequestMapping(method=RequestMethod.GET,params="getHomework")
	public void getHomeworks(HttpServletRequest request,HttpServletResponse response){
		User user = (User) request.getSession().getAttribute("user");
		List<Homework> homeworks = homeworkService.getHomeworks(user.getUsername(),user.getUserStatus());
		CommonUtil.sendJsonArray(response,homeworks);
	}
	
	/**
	 * 获取该用户的所有可以发布作业的科目
	 * @param request
	 * @param response
	 */
	@RequestMapping(method=RequestMethod.GET,params="getSubjects")
	public void getSubjects(HttpServletRequest request,HttpServletResponse response){
		User user = (User) request.getSession().getAttribute("user");
		List<String> subjects = homeworkService.getSubjects(user.getUsername(),user.getUserStatus());
		CommonUtil.sendJsonArray(response,subjects);
	}
	
	/**
	 * 获取其他用户的发布作业的科目
	 */
	@RequestMapping(method=RequestMethod.GET,params="getOthersSubjects")
	public void getOthersSubject(HttpServletRequest request,HttpServletResponse response){
		String username = request.getParameter("othersName");
		try {
			username = new String(username.getBytes("iso8859-1"),"UTF-8");
		} catch (UnsupportedEncodingException e) {e.printStackTrace();}
		String userStatusString = request.getParameter("userStatus");
		int userStatus  = Integer.parseInt(userStatusString);
		List<String> subjects = homeworkService.getSubjects(username,userStatus);
		CommonUtil.sendJsonArray(response,subjects);
	}
	
	/**
	 * 用户发表评论
	 * @param request
	 * @param response
	 */
	@RequestMapping(method=RequestMethod.POST,params="sendComment")
	public ModelAndView sendComment(HttpServletRequest request,HttpServletResponse response){
		User user = (User) request.getSession().getAttribute("user");
		String comment = request.getParameter("comment");
		String homeworkId= request.getParameter("homeworkId");
		CommentEntity commentEntity = new CommentEntity();
		commentEntity.setComment(comment);
		commentEntity.setHomeworkId(homeworkId);
		commentEntity.setUsername(user.getUsername());
		commentEntity.setUserStatus(user.getUserStatus());
		commentEntity.setCommentTime(new SimpleDateFormat("yyyy-MM-dd hh:mm:ss").format(new Date()));
		homeworkService.saveCommentEntity(commentEntity);
		ModelAndView modelAndView = new ModelAndView("main");
		return modelAndView;
	}
	
}
