import bpy, math, os, shutil, json
from mathutils import Vector, Quaternion
from collections import defaultdict
base='C:/Users/rober/Joshua/'
bpy.ops.wm.open_mainfile(filepath=base+'Logs/BuzzardWork/source.blend')
bird=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.ops.object.select_all(action='DESELECT');bird.select_set(True);bpy.context.view_layer.objects.active=bird
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
original_vertices=len(bird.data.vertices)
# A lighter game copy; UV coordinates remain attached to the surface.
dec=bird.modifiers.new('Flock performance mesh','DECIMATE');dec.ratio=.10
bpy.ops.object.modifier_apply(modifier=dec.name)
bird.name='BuzzardBody'
texpath=base+'Assets/BuzzardFlight/BuzzardColor.png'
shutil.copy2(r'C:/Users/rober/Downloads/Meshy_AI_Create_a_realistic_g_1022174120_texture_fbx/Meshy_AI_Create_a_realistic_g_1022174120_texture_fbx/Meshy_AI_Create_a_realistic_g_1022174120_texture.png',texpath)
mat=bpy.data.materials.new('BuzzardColor');mat.use_nodes=True
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(texpath)
mat.node_tree.links.new(tex.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.85
bird.data.materials.clear();bird.data.materials.append(mat)
armdata=bpy.data.armatures.new('BuzzardSkeleton');rig=bpy.data.objects.new('BuzzardRig',armdata);bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);bird.select_set(False);bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
 b=armdata.edit_bones.new(name);b.head=head;b.tail=tail
 if parent:b.parent=armdata.edit_bones[parent]
bone('Root',(0,0,0),(0,0,.1))
bone('Body',(0,0,0),(0,0,.19),'Root')
bone('Neck',(0,-.035,.13),(0,-.06,.24),'Body')
bone('Head',(0,-.06,.24),(0,-.20,.27),'Neck')
bone('Tail',(0,.08,-.10),(0,.18,-.27),'Body')
for side,s in [('L',1),('R',-1)]:
 bone('Wing.'+side,(s*.12,0,.16),(s*.47,0,.16),'Body')
 bone('WingTip.'+side,(s*.47,0,.16),(s*.94,0,.16),'Wing.'+side)
 bone('Thigh.'+side,(s*.066,.005,-.09),(s*.066,.01,-.22),'Body')
 bone('Leg.'+side,(s*.066,.01,-.22),(s*.066,0,-.31),'Thigh.'+side)
 bone('Foot.'+side,(s*.066,0,-.31),(s*.066,-.09,-.33),'Leg.'+side)
bpy.ops.object.mode_set(mode='OBJECT')
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
buckets=defaultdict(list)
for v in bird.data.vertices:
 x,y,z=v.co;ax=abs(x);side='L' if x>=0 else 'R';weights={}
 wing=smooth(.115,.235,ax)
 leg=(1-smooth(-.11,-.045,z))*(1-smooth(.13,.17,y))*(1-smooth(.18,.23,ax))*(1-wing)
 head=smooth(.13,.205,z)*(1-smooth(.10,.17,ax))*(1-wing)
 tail=smooth(.07,.13,y)*(1-smooth(-.10,.04,z))*(1-wing)
 total=wing+leg+head+tail
 if total>1:wing/=total;leg/=total;head/=total;tail/=total
 tip=smooth(.40,.58,ax);weights['Wing.'+side]=wing*(1-tip);weights['WingTip.'+side]=wing*tip
 foot=1-smooth(-.31,-.285,z);shin=1-smooth(-.225,-.18,z)
 weights['Thigh.'+side]=leg*(1-shin);weights['Leg.'+side]=leg*shin*(1-foot);weights['Foot.'+side]=leg*shin*foot
 neck=1-smooth(.21,.255,z);weights['Neck']=head*neck;weights['Head']=head*(1-neck);weights['Tail']=tail
 weights['Body']=max(0,1-wing-leg-head-tail)
 # Quantization batches assignment without millions of API calls.
 for name,w in weights.items():
  level=round(w*100)
  if level:buckets[(name,level)].append(v.index)
for b in armdata.bones:bird.vertex_groups.new(name=b.name)
for (name,w),indices in buckets.items():bird.vertex_groups[name].add(indices,w/100,'REPLACE')
modifier=bird.modifiers.new('Flight rig','ARMATURE');modifier.object=rig;bird.parent=rig
for p in rig.pose.bones:p.rotation_mode='QUATERNION'
def rotate(name,axis,degrees):
 b=rig.pose.bones[name];local=b.bone.matrix_local.to_quaternion().inverted()@Vector(axis);b.rotation_quaternion=Quaternion(local,math.radians(degrees))
def pose(flight,flap=0,fold=0):
 for p in rig.pose.bones:p.rotation_quaternion=Quaternion();p.location=(0,0,0);p.scale=(1,1,1)
 rotate('Body',(1,0,0),78*flight)
 rotate('Neck',(1,0,0),-35*flight);rotate('Head',(1,0,0),-32*flight)
 for side,s in [('L',1),('R',-1)]:
  rotate('Thigh.'+side,(1,0,0),-25*flight);rotate('Leg.'+side,(1,0,0),50*flight);rotate('Foot.'+side,(1,0,0),65*flight);rig.pose.bones['Thigh.'+side].scale=(1-.25*flight,1-.35*flight,1-.25*flight)
  b=rig.pose.bones['Wing.'+side];q=b.bone.matrix_local.to_quaternion();b.rotation_quaternion=Quaternion(q.inverted()@Vector((0,0,1)),math.radians(s*flap))@Quaternion(q.inverted()@Vector((0,1,0)),math.radians(s*fold))
  rotate('WingTip.'+side,(0,0,1),s*flap*.35)
 rotate('Tail',(1,0,0),-10*flight)
def key(frame):
 for p in rig.pose.bones:p.keyframe_insert('rotation_quaternion',frame=frame,group=p.name);p.keyframe_insert('location',frame=frame,group=p.name);p.keyframe_insert('scale',frame=frame,group=p.name)
scene=bpy.context.scene;scene.render.fps=30
for name,end in [('Fly',31),('Glide',61),('Land',61),('TakeOff',61),('Perch',61),('Hop',31)]:
 rig.animation_data_create();action=bpy.data.actions.new(name);rig.animation_data.action=action
 for f in range(1,end+1,2):
  t=(f-1)/(end-1)
  if name=='Fly':pose(1,26*math.sin(t*math.tau))
  elif name=='Glide':pose(1,2*math.sin(t*math.tau))
  elif name=='Land':pose(1-smooth(.15,.85,t),30*math.sin(t*math.tau*2)*(1-t),55*smooth(.65,1,t))
  elif name=='TakeOff':pose(smooth(.05,.70,t),36*math.sin(t*math.tau*3)*smooth(0,.2,t),55*(1-smooth(0,.3,t)))
  elif name=='Hop':
   pose(0,0,48);rotate('Body',(1,0,0),-8*math.sin(t*math.tau));rig.pose.bones['Root'].location.z=.035*max(0,math.sin(t*math.tau))
  else:pose(0,0,55)
  key(f)
 track=rig.animation_data.nla_tracks.new();track.name=name;strip=track.strips.new(name,1,action);track.mute=True
 action.use_fake_user=True
rig.animation_data.action=bpy.data.actions['Glide'];scene.frame_set(1)
# Export each named action as a separate FBX take, without an extra default take.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bird.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=base+'Assets/BuzzardFlight/BuzzardRigged.fbx',use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='RELATIVE')
bpy.ops.wm.save_as_mainfile(filepath=base+'Logs/BuzzardWork/BuzzardRigged.blend')
print('RIG_SUMMARY',json.dumps({'original_vertices':original_vertices,'game_vertices':len(bird.data.vertices),'triangles':sum(len(p.vertices)-2 for p in bird.data.polygons),'bones':len(armdata.bones),'clips':['Fly','Glide','Land','TakeOff','Perch'],'uv_layers':len(bird.data.uv_layers)}),flush=True)
camera=scene.camera;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.color_type='TEXTURE'
for name,clip,frame,view,zoom in [('rig-flight','Glide',1,(1,-1,.5),2.15),('rig-side','Glide',1,(1,0,0),1),('rig-perch','Perch',1,(1,-1,.3),1.6),('rig-flap','Fly',9,(0,-1,.4),2.15)]:
 rig.animation_data.action=bpy.data.actions[clip];scene.frame_set(frame);camera.location=Vector(view).normalized()*6;camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=zoom;scene.render.filepath=base+'Logs/BuzzardWork/'+name+'.png';bpy.ops.render.render(write_still=True)




