package com.shusheng.service.impl;

import java.util.List;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.orm.hibernate3.HibernateTemplate;
import org.springframework.stereotype.Component;

import com.shusheng.dao.HomeworkDao;
import com.shusheng.model.CommentEntity;
import com.shusheng.model.FocusMsg;
import com.shusheng.model.Homework;
import com.shusheng.model.Subject;

@Component("homeworkDao")
public class HomeworkDaoImpl implements HomeworkDao {
	
	@Autowired
	private HibernateTemplate hibernateTemplate;

	/**
	 * 获取作业
	 */
	@SuppressWarnings("unchecked")
	@Override
	public List<Homework> getHomeworks(String username, int userStatus) {
		List<Homework> homeworks = null;
		//先查出自己发布的作业
		homeworks = hibernateTemplate.find("from Homework hw where hw.publishUsername=? and userstatus=? order by endTime",username,userStatus);
		//再查出自己关注的人的作业
		List<FocusMsg> focusMsgs = hibernateTemplate.find("from FocusMsg fm where username=? and userStatus=?",username,userStatus);
		for(FocusMsg focusMsg : focusMsgs) {//当前用户关注的信息
			//以下是被关注人的信息
			String subjectName = focusMsg.getSubjectName();
			int focusPStatus = focusMsg.getFocusPStatus();
			String focusedName = focusMsg.getFocusedName();
			//通过上面被关注的信息获取作业
			List<Homework> tempHomeworks = hibernateTemplate.find("from Homework hw where hw.publishUsername=? and userstatus=? and subject=? order by endTime",focusedName,focusPStatus,subjectName);
			for (int i = 0; i < tempHomeworks.size(); i++) {
				homeworks.add(tempHomeworks.get(i));
			}
		}
		//获取班级公告
		List<Homework> homeworks2 = hibernateTemplate.find("select hw from Homework hw,Student stu where hw.subject='班级公告' and hw.classStr=stu.classStr and stu.username=?",username);
		for (Homework homework : homeworks2) {
			homeworks.add(homework);
		}
		return homeworks;
	}

	/**
	 * 获取发布作业的科目
	 */
	@SuppressWarnings("unchecked")
	@Override
	public List<String> getSubjects(String username, int userStatus) {
		List<String> list = null;
		if (userStatus==0) {
			list = hibernateTemplate.find("select sub.name from Subject sub,Student stu where sub.userId=stu.id and stu.username=?",username);
		}else {
			list = hibernateTemplate.find("select sub.name from Subject sub,Teacher tea where sub.userId=tea.id and tea.username=?",username);
		}
		return list;
	}

	/**
	 * 添加一次作业
	 */
	@Override
	public boolean save(Homework homework) {
		hibernateTemplate.save(homework);
		return true;
	}

	/**
	 * 存储发布作业科目的信息
	 */
	@Override
	public void saveSubject(List<Subject> subjectList) {
		for (int i = 0; i < subjectList.size(); i++) {
			hibernateTemplate.save(subjectList.get(i));
		}
	}

	/**
	 * 存储一个评论
	 */
	@Override
	public void saveCommentEntity(CommentEntity commentEntity) {
		hibernateTemplate.save(commentEntity);
	}
}
