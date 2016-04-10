package com.shusheng.dao.impl;

import java.util.List;
import javax.annotation.Resource;
import org.springframework.orm.hibernate3.HibernateTemplate;
import org.springframework.stereotype.Component;
import com.shusheng.dao.UserDao;
import com.shusheng.model.FocusMsg;
import com.shusheng.model.Student;
import com.shusheng.model.Subject;
import com.shusheng.model.Teacher;
import com.shusheng.util.Constant;

@Component("userDao")
public class UserDaoImpl implements UserDao{

	@Resource
	private HibernateTemplate hibernateTemplate;
	/**
	 * 返回0表示登录成功，返回1表示用户名不存在，返回2表示密码错误
	 */
	@Override
	public int doLogin(String username, String password,int status) {
		@SuppressWarnings("rawtypes")
		List list = null;
		String table=null;
		if (status==Constant.studentStatus)//学生
			table = "Student";
		else 
			table = "Teacher";
		list = hibernateTemplate.find("select count(*) from "+table +" where username=? and password=?",username,password);
		int temp  = Integer.parseInt(list.get(0).toString());
		if (temp > 0){//用户登录成功
			return 0;
		} else //检查时用户名不存在还是密码错误
			list = hibernateTemplate.find("select count(*) from "+table+" where username=?", username);
		temp  = Integer.parseInt(list.get(0).toString());
		if (temp ==0)//用户名不存在
			return 1;
		else
			return 2;//密码错误
	}
	
	/**
	 * 返回true表示注册成功，false表示注册失败，用户名已存在
	 */
	@Override
	public boolean doRegister(String username, String password, int userStatus) {
		String table = null;
		if (userStatus==0) {//学生
			table="Student";
		}else 
			table="Teacher";
		List list = hibernateTemplate.find("select count(username) from "+table+" where username=?",username);
		Integer count = Integer.parseInt(list.get(0).toString());
		if (count==0){ //不存在该用户，就存储该用户
			if(userStatus==0){//存储学生
				Student student = new Student();
				student.setUsername(username);
				student.setPassword(password);
				hibernateTemplate.save(student);
			}else {//存储教师
				Teacher teacher = new Teacher();
				teacher.setUsername(username);
				teacher.setPassword(password);
				hibernateTemplate.save(teacher);
			}
			return true;
		}
		return false;
	}

	
	/**
	 * 更新一个对象
	 */
	@Override
	public void update(Object object) {
		hibernateTemplate.update(object);
	}

	/**
	 * 存储关注科目的信息
	 */
	@Override
	public void saveFocusMsg(List<FocusMsg> focusMsgs) {
		for (int i = 0; i < focusMsgs.size(); i++) {
			hibernateTemplate.save(focusMsgs.get(i));
		}
	}



	
	/**
	 * 获取一个用户对象
	 */
	@Override
	public Object getUser(String username, String password, int status) {
		@SuppressWarnings("rawtypes")
		List list = null;
		String table=null;
		if (status==0)//学生
			table = "Student";
		else 
			table = "Teacher";
		list = hibernateTemplate.find("from "+table +" where username=? and password=?",username,password);
		if (list==null) {
			return list;
		}
		return list.get(0);
	}
}
