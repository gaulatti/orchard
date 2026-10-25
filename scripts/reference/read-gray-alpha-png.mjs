import assert from 'node:assert/strict';
import {inflateSync} from 'node:zlib';

// Decode the pinned 8-bit grayscale/alpha asset without changing its bytes.
// Native capture comparisons continue to use the separate RGB/RGBA decoder.
export function readGrayAlphaPng(bytes) {
 assert.equal(bytes.subarray(0,8).toString('hex'),'89504e470d0a1a0a');
 const crc32=data=>{let crc=0xffffffff;for(const byte of data){crc^=byte;for(let bit=0;bit<8;bit++)crc=(crc>>>1)^((crc&1)?0xedb88320:0);}return(crc^0xffffffff)>>>0;};
 let width,height,ended=false;const parts=[];
 for(let offset=8;offset<bytes.length;){
  assert(offset+12<=bytes.length);const length=bytes.readUInt32BE(offset),end=offset+8+length;assert(end+4<=bytes.length);const type=bytes.toString('ascii',offset+4,offset+8),data=bytes.subarray(offset+8,end);assert.equal(crc32(bytes.subarray(offset+4,end)),bytes.readUInt32BE(end),'PNG CRC differs');
  if(type==='IHDR'){assert.equal(width,undefined);assert.equal(length,13);width=data.readUInt32BE(0);height=data.readUInt32BE(4);assert(width>0&&height>0);assert.equal(data[8],8);assert.equal(data[9],4,'Expected the original grayscale/alpha asset');assert.deepEqual([...data.subarray(10)],[0,0,0]);}
  else if(type==='IDAT'){assert(width);parts.push(data);}
  else if(type==='IEND'){assert.equal(length,0);assert.equal(end+4,bytes.length);ended=true;break;}
  offset=end+4;
 }
 assert(ended&&parts.length);const stride=width*2,encoded=inflateSync(Buffer.concat(parts));assert.equal(encoded.length,(stride+1)*height);const raw=Buffer.alloc(width*height*2);let cursor=0;
 const paeth=(a,b,c)=>{const p=a+b-c,da=Math.abs(p-a),db=Math.abs(p-b),dc=Math.abs(p-c);return da<=db&&da<=dc?a:db<=dc?b:c;};
 for(let y=0;y<height;y++){const filter=encoded[cursor++];assert(filter<=4);for(let x=0;x<stride;x++){const at=y*stride+x,left=x>=2?raw[at-2]:0,above=y?raw[at-stride]:0,corner=y&&x>=2?raw[at-stride-2]:0;const p=filter===0?0:filter===1?left:filter===2?above:filter===3?Math.floor((left+above)/2):paeth(left,above,corner);raw[at]=(encoded[cursor++]+p)&255;}}
 const alpha=Buffer.alloc(width*height),gray=Buffer.alloc(width*height);for(let i=0;i<alpha.length;i++){gray[i]=raw[i*2];alpha[i]=raw[i*2+1];}return{width,height,alpha,gray};
}
